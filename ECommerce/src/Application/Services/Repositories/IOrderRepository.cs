using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services.Repositories
{
    // Order için repository sözleşmesi (bkz. IProductRepository).
    // Order bir "aggregate root"tur: kalemleri (Items) her zaman siparişle birlikte yüklenir.
    public interface IOrderRepository
    {
        // Okuma amaçlı: takip yapılmaz, kalemlerle birlikte, en yeni sipariş önce gelir.
        Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default);

        // Kalemlerle (Items) birlikte döner.
        // tracking: true => güncelleme/silme için, tracking: false => yalnızca okuma için.
        Task<Order?> GetByIdAsync(Guid id, bool tracking = true, CancellationToken cancellationToken = default);

        Task AddAsync(Order order, CancellationToken cancellationToken = default);

        void Remove(Order order);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
