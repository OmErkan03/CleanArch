using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services.Repositories
{
    // Kullanıcı (User) için veri erişim sözleşmesi.
    // Implementasyonu Persistence katmanındadır (bkz. Persistence/Repositories/UserRepository.cs).
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        // Değişiklik takibi açık döner (ör. MFA kurulumunda kullanıcı güncellenir).
        Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

        // Kullanıcıyı yetkileriyle (OperationClaims) birlikte, değişiklik takibi AÇIK olarak getirir.
        // Yetki ekleme/çıkarma (ara tabloya INSERT/DELETE) için koleksiyonun yüklenmiş ve takip ediliyor olması gerekir.
        Task<User?> GetByIdWithOperationClaimsAsync(Guid userId, CancellationToken cancellationToken = default);

        // Kullanıcının sahip olduğu operation claim adları (ör. "Product.Read"). Redis'teki oturuma yazılır.
        Task<List<string>> GetOperationClaimNamesAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);

        // Değişiklikleri veritabanına yansıtır (INSERT / UPDATE / DELETE burada çalışır).
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
