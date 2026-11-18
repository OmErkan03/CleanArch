using ECommerce.Domain.Entities;
using ECommerce.Application.Services.Repositories;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IOrderRepository'nin EF Core ile yazılmış implementasyonu.
    public class OrderRepository : IOrderRepository
    {
        private readonly UludagDbContext _context;

        public OrderRepository(UludagDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<Order?> GetByIdAsync(Guid id, bool tracking = true, CancellationToken cancellationToken = default)
        {
            // FindAsync Include desteklemediği için kalemlerle birlikte sorgu ile yüklüyoruz.
            var query = _context.Orders.Include(o => o.Items).AsQueryable();
            if (!tracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            await _context.Orders.AddAsync(order, cancellationToken);
        }

        public void Remove(Order order)
        {
            _context.Orders.Remove(order);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
