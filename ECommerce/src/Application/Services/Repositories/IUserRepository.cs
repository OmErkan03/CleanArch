using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services.Repositories
{
    // Kullanıcı (User) için veri erişim sözleşmesi.
    // Implementasyonu Persistence katmanındadır (bkz. Persistence/Repositories/UserRepository.cs).
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        // Kullanıcının sahip olduğu operation claim adları (ör. "Product.Read"). Login'de token'a yazılır.
        Task<List<string>> GetOperationClaimNamesAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);

        // Değişiklikleri veritabanına yansıtır (INSERT / UPDATE / DELETE burada çalışır).
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
