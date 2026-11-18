using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Seeds
{
    // Başlangıç ürün verileri.
    // DbContext options'taki UseSeeding / UseAsyncSeeding üzerinden çağrılır (bkz. PersistenceServiceRegistration).
    // EF bunları "dotnet ef database update", context.Database.Migrate() ve EnsureCreated() sırasında otomatik çalıştırır.
    //
    // HasData yerine bu yöntemin kullanılma sebebi: Price bir ComplexProperty (Money) ve
    // EF Core HasData ile complex property seed etmeyi desteklemiyor (efcore#31254).
    internal static class ProductSeed
    {
        // Id'ler SABİT verilir: seed her çalıştığında aynı kaydı tanıyıp tekrar eklememek için.
        // (Guid.NewGuid() kullanılsaydı her çalıştırmada yeni bir kopya eklenirdi.)
        // Yeni seed ürünü eklerken yeni, sabit bir Guid üretip listeye ekleyin.
        private static List<Product> GetProducts() =>
        [
            new()
            {
                Id = new Guid("3f2b8c1e-6d4a-4b7e-9a51-2c8e7f0d1a63"),
                Name = "Test Product",
                Price = new Money(100, "USD"),
                Stock = 10
            }
        ];

        // Senkron sürüm: context.Database.Migrate() / EnsureCreated() için.
        public static void Seed(DbContext context)
        {
            var products = context.Set<Product>();
            var existingIds = products.Select(p => p.Id).ToHashSet();

            // Yalnızca veritabanında henüz olmayan seed kayıtları eklenir => tekrar tekrar çalıştırmak güvenlidir (idempotent).
            var missing = GetProducts().Where(p => !existingIds.Contains(p.Id)).ToList();
            if (missing.Count == 0)
                return;

            products.AddRange(missing);
            context.SaveChanges();
        }

        // Asenkron sürüm: "dotnet ef database update", MigrateAsync() / EnsureCreatedAsync() için.
        public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
        {
            var products = context.Set<Product>();
            var existingIds = (await products.Select(p => p.Id).ToListAsync(cancellationToken)).ToHashSet();

            var missing = GetProducts().Where(p => !existingIds.Contains(p.Id)).ToList();
            if (missing.Count == 0)
                return;

            await products.AddRangeAsync(missing, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
