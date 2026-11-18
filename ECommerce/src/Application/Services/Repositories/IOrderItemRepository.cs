using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services.Repositories
{
    // OrderItem için repository sözleşmesi (bkz. IProductRepository).
    public interface IOrderItemRepository
    {
        // Okuma amaçlı: takip yapılmaz.
        Task<List<OrderItem>> GetAllAsync(CancellationToken cancellationToken = default);

        // tracking: true => güncelleme/silme için, tracking: false => yalnızca okuma için.
        Task<OrderItem?> GetByIdAsync(Guid id, bool tracking = true, CancellationToken cancellationToken = default);

        Task AddAsync(OrderItem orderItem, CancellationToken cancellationToken = default);

        void Remove(OrderItem orderItem);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
