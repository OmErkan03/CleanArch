using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services.Repositories
{
    // Repository Pattern => veri erişimini bir arayüzün arkasına saklar.
    // Handler'lar EF Core'u / DbContext'i bilmez, yalnızca bu sözleşmeyi bilir.
    // Dependency Inversion: arayüz Application'da, implementasyonu Persistence'ta durur.
    // Application, Persistence'ı referans almaz; tam tersi Persistence, Application'a bağımlıdır.
    // Böylece veritabanı değişse bile (PostgreSQL -> SQL Server, EF -> Dapper vb.) Application etkilenmez.
    public interface IProductRepository
    {
        // Okuma amaçlı: takip (tracking) yapılmaz, isme göre sıralı döner.
        Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default);

        // tracking: true => dönen nesne EF tarafından takip edilir (güncelleme/silme için gerekli).
        // tracking: false => yalnızca okuma için, daha performanslı.
        Task<Product?> GetByIdAsync(Guid id, bool tracking = true, CancellationToken cancellationToken = default);

        Task AddAsync(Product product, CancellationToken cancellationToken = default);

        void Remove(Product product);

        // Değişiklikleri veritabanına yansıtır (INSERT / UPDATE / DELETE burada çalışır).
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
