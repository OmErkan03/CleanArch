using ECommerce.Domain.Entities;
using ECommerce.Application.Services.Repositories;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IOrderItemRepository'nin EF Core ile yazılmış implementasyonu.
    public class OrderItemRepository : IOrderItemRepository
    {
        private readonly UludagDbContext _context;

        public OrderItemRepository(UludagDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderItem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.OrderItems
                .AsNoTracking()
                .OrderBy(i => i.OrderId)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrderItem?> GetByIdAsync(Guid id, bool tracking = true, CancellationToken cancellationToken = default)
        {
            if (tracking)
            {
                return await _context.OrderItems.FindAsync([id], cancellationToken);
            }

            return await _context.OrderItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public async Task AddAsync(OrderItem orderItem, CancellationToken cancellationToken = default)
        {
            await _context.OrderItems.AddAsync(orderItem, cancellationToken);
        }

        public void Remove(OrderItem orderItem)
        {
            _context.OrderItems.Remove(orderItem);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
