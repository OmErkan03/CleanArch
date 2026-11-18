using ECommerce.Domain.Entities;
using ECommerce.Application.Services.Repositories;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IProductRepository'nin EF Core ile yazılmış implementasyonu.
    // DbContext yalnızca burada kullanılır; handler'lar doğrudan context'e erişmez.
    public class ProductRepository : IProductRepository
    {
        private readonly UludagDbContext _context;

        public ProductRepository(UludagDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // AsNoTracking => EF okunan nesneleri takip etmez; sadece okuma yapıldığı için
            // gereksiz takip maliyetinden kurtuluruz.
            return await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Product?> GetByIdAsync(Guid id, bool tracking = true, CancellationToken cancellationToken = default)
        {
            if (tracking)
            {
                // FindAsync => önce EF'in takip ettiği nesnelere, yoksa veritabanına Id ile bakar.
                return await _context.Products.FindAsync([id], cancellationToken);
            }

            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            // AddAsync sırasında EF, Guid tipindeki Id'yi otomatik üretir.
            await _context.Products.AddAsync(product, cancellationToken);
        }

        public void Remove(Product product)
        {
            // Remove => nesneyi "silinecek" olarak işaretler, DELETE sorgusu SaveChanges'te çalışır.
            _context.Products.Remove(product);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);

        public async Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
        }
    }
}
