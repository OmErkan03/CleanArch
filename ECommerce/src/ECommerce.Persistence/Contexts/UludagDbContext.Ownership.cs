using System.Reflection;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Uludag.Core.Exceptions;
using Uludag.Core.Security.Ownership;

namespace ECommerce.Persistence.Contexts
{
    // IDOR (Insecure Direct Object Reference) koruması.
    //
    // Handler'ların / repository'lerin "bu kayıt bu kullanıcıya mı ait?" kontrolünü tek tek yazması unutulmaya çok açıktır.
    // Bu yüzden kontrol veri katmanında, merkezi ve otomatik yapılır:
    //
    // 1) OKUMA => Global query filter ("Ownership"):
    //    IOwnedEntity olan her entity sorgusuna "UserId = oturumdaki kullanıcı" koşulu otomatik eklenir
    //    (Find, Include, Any, Count, ExecuteUpdate/ExecuteDelete dahil). Başkasının kaydı "yokmuş gibi" davranır:
    //    GetById null döner => iş kuralı 404 fırlatır. 403 yerine 404 dönmek kaydın varlığını da sızdırmaz.
    //    Sahibi bir üst kayıt üzerinden belirlenen entity'ler (OrderItem => Order) için filtre ayrıca tanımlanır.
    //
    // 2) YAZMA => SaveChanges kontrolü (derinlemesine savunma):
    //    Filtre yanlışlıkla atlansa bile (IgnoreQueryFilters, elle Attach edilen nesne vb.)
    //    başkasına ait kayıt eklenemez / güncellenemez / silinemez, kaydın sahibi değiştirilemez => ForbiddenException (403).
    //
    // "{EntityAdı}.Admin" veya genel "Admin" claim'i olan kullanıcılar için her iki kontrol de devre dışıdır.
    // Oturum yoksa (UserId == null) sahipli hiçbir kayda erişilemez (fail-closed).
    //
    // Yeni bir sahipli entity eklemek için: entity'ye IOwnedEntity uygulamak yeterlidir.
    // Sahibi üst kayıttan gelen bir entity eklenirse ApplyOwnershipFilters ve EnforceOwnershipOnWrite'a eklenmelidir.
    public partial class UludagDbContext
    {
        // Filtre adı: gerekirse yalnızca bu filtre kapatılabilir => query.IgnoreQueryFilters([OwnershipFilter]).
        // DİKKAT: Kapatmak IDOR korumasını kaldırır; yalnızca sistem işlemlerinde ve bilinçli olarak kullanılmalıdır.
        public const string OwnershipFilter = "Ownership";

        private readonly IDataOwnershipContext? _ownership;

        // Query filter'lar bu üyeleri kullanır. EF bunları her sorguda o anki DbContext örneğinden parametre olarak okur;
        // yani model bir kez oluşturulsa da filtre her istekte o isteğin kullanıcısıyla çalışır.
        private Guid? CurrentUserId => _ownership?.UserId;
        private bool CanAccessAllOf(string resource) => _ownership?.CanAccessAllOf(resource) ?? false;

        private static readonly MethodInfo ApplyOwnedEntityFilterMethod =
            typeof(UludagDbContext).GetMethod(nameof(ApplyOwnedEntityFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private void ApplyOwnershipFilters(ModelBuilder modelBuilder)
        {
            // IOwnedEntity uygulayan tüm entity'ler otomatik bulunur.
            var ownedEntityTypes = modelBuilder.Model.GetEntityTypes()
                .Select(t => t.ClrType)
                .Where(t => typeof(IOwnedEntity).IsAssignableFrom(t))
                .ToList();

            foreach (var clrType in ownedEntityTypes)
                ApplyOwnedEntityFilterMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);

            // OrderItem'ın kendi UserId'si yoktur; sahibi bağlı olduğu siparişin sahibidir.
            // "i.Order.UserId" yerine EXISTS kullanılır: navigation üzerinden yazılırsa INNER JOIN, Order'ın kendi filtresiyle
            // birleşip yalnızca OrderItem.Admin olan kullanıcının kalemlerini de gizlerdi.
            modelBuilder.Entity<OrderItem>().HasQueryFilter(OwnershipFilter, i =>
                CanAccessAllOf(nameof(Order))
                || CanAccessAllOf(nameof(OrderItem))
                || Orders.Any(o => o.Id == i.OrderId && o.UserId == CurrentUserId));
        }

        private void ApplyOwnedEntityFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class, IOwnedEntity
        {
            var resource = typeof(TEntity).Name;
            modelBuilder.Entity<TEntity>().HasQueryFilter(OwnershipFilter, e =>
                CanAccessAllOf(resource) || e.UserId == CurrentUserId);
        }

        private void EnforceOwnershipOnWrite()
        {
            CheckOwnedEntities();

            var orderIds = CollectOrderIdsOfChangedOrderItems();
            if (orderIds.Count == 0)
                return;

            // Filtreli sorgu: yalnızca kullanıcının erişebildiği siparişler döner.
            var accessibleCount = Orders.Count(o => orderIds.Contains(o.Id));
            EnsureAllOrdersAccessible(orderIds, accessibleCount);
        }

        private async Task EnforceOwnershipOnWriteAsync(CancellationToken cancellationToken)
        {
            CheckOwnedEntities();

            var orderIds = CollectOrderIdsOfChangedOrderItems();
            if (orderIds.Count == 0)
                return;

            var accessibleCount = await Orders.CountAsync(o => orderIds.Contains(o.Id), cancellationToken);
            EnsureAllOrdersAccessible(orderIds, accessibleCount);
        }

        private void CheckOwnedEntities()
        {
            foreach (var entry in ChangeTracker.Entries<IOwnedEntity>())
            {
                if (entry.State is EntityState.Unchanged or EntityState.Detached)
                    continue;

                if (CanAccessAllOf(entry.Metadata.ClrType.Name))
                    continue;

                var userIdProperty = entry.Property(e => e.UserId);
                var currentUserId = CurrentUserId ?? throw new ForbiddenException();

                switch (entry.State)
                {
                    case EntityState.Added:
                        // Sahibi belirtilmemişse oturumdaki kullanıcı atanır; başkası adına kayıt oluşturulamaz.
                        if (userIdProperty.CurrentValue == Guid.Empty)
                            userIdProperty.CurrentValue = currentUserId;
                        else if (userIdProperty.CurrentValue != currentUserId)
                            throw new ForbiddenException();
                        break;

                    case EntityState.Modified:
                        // Hem kaydın asıl sahibi hem (değiştirildiyse) yeni sahibi kullanıcının kendisi olmalıdır:
                        // başkasının kaydı değiştirilemez, kendi kaydı başkasına devredilemez.
                        if (userIdProperty.OriginalValue != currentUserId || userIdProperty.CurrentValue != currentUserId)
                            throw new ForbiddenException();
                        break;

                    case EntityState.Deleted:
                        if (userIdProperty.OriginalValue != currentUserId)
                            throw new ForbiddenException();
                        break;
                }
            }
        }

        // Eklenen / güncellenen / silinen kalemlerin bağlı olduğu (ve güncellemede taşındığı) siparişler.
        private List<Guid> CollectOrderIdsOfChangedOrderItems()
        {
            if (CanAccessAllOf(nameof(Order)) || CanAccessAllOf(nameof(OrderItem)))
                return [];

            var ids = new HashSet<Guid>();
            foreach (var entry in ChangeTracker.Entries<OrderItem>())
            {
                if (entry.State is EntityState.Unchanged or EntityState.Detached)
                    continue;

                if (CurrentUserId is null)
                    throw new ForbiddenException();

                PropertyEntry<OrderItem, Guid> orderId = entry.Property(i => i.OrderId);
                ids.Add(orderId.CurrentValue);
                if (entry.State != EntityState.Added)
                    ids.Add(orderId.OriginalValue);
            }

            // Aynı SaveChanges'te eklenen siparişler henüz veritabanında yoktur; sahiplikleri CheckOwnedEntities'te doğrulandı.
            foreach (var order in ChangeTracker.Entries<Order>().Where(e => e.State == EntityState.Added))
                ids.Remove(order.Entity.Id);

            return [.. ids];
        }

        private static void EnsureAllOrdersAccessible(List<Guid> orderIds, int accessibleCount)
        {
            if (accessibleCount != orderIds.Count)
                throw new ForbiddenException();
        }
    }
}
