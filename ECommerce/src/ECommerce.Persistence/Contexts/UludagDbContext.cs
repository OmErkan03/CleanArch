using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Uludag.Core.Security.Ownership;

namespace ECommerce.Persistence.Contexts
{
    // IDOR korumasıyla ilgili kısım (query filter'lar ve SaveChanges kontrolü) UludagDbContext.Ownership.cs'tedir.
    public partial class UludagDbContext : DbContext
    {
        public UludagDbContext(DbContextOptions options, IDataOwnershipContext ownership) : base(options)
        {
            _ownership = ownership;
        }
        protected UludagDbContext()
        {

        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<User> Users => Set<User>();
        public DbSet<OperationClaim> OperationClaims => Set<OperationClaim>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UludagDbContext).Assembly);
            ApplyOwnershipFilters(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            EnforceOwnershipOnWrite();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            await EnforceOwnershipOnWriteAsync(cancellationToken);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
