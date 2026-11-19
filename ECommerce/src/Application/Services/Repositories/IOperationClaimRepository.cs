using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services.Repositories
{
    // OperationClaim için veri erişim sözleşmesi.
    // Implementasyonu Persistence katmanındadır (bkz. Persistence/Repositories/OperationClaimRepository.cs).
    public interface IOperationClaimRepository
    {
        // Değişiklik takibi açık döner: kullanıcıya eklenirken EF'in bu claim'i "yeni kayıt" sanmaması için gereklidir.
        Task<OperationClaim?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
