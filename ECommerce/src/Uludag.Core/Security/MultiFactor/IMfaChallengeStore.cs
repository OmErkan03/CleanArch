namespace Uludag.Core.Security.MultiFactor
{
    public interface IMfaChallengeStore
    {
        // Challenge'ı kaydeder ve istemciye verilecek MFA token'ını (anahtar) döndürür.
        Task<string> CreateAsync(MfaChallenge challenge, CancellationToken cancellationToken = default);

        // Yoksa / süresi dolmuşsa / kullanılmışsa null döner.
        Task<MfaChallenge?> GetAsync(string mfaToken, CancellationToken cancellationToken = default);

        // Kalan süreyi değiştirmeden günceller (bekleyen gizli anahtar, hatalı deneme sayısı).
        Task<bool> UpdateAsync(string mfaToken, MfaChallenge challenge, CancellationToken cancellationToken = default);

        // MFA tamamlandığında ya da deneme sınırı aşıldığında token tek kullanımlık olsun diye silinir.
        Task RemoveAsync(string mfaToken, CancellationToken cancellationToken = default);

        // Kullanıcının yarım kalmış (şifresi doğrulanmış, MFA adımı tamamlanmamış) tüm girişlerini iptal eder (logout).
        // İptal edilen MFA token sayısını döndürür.
        Task<int> RemoveUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default);

        // Bir TOTP kodu geçerlilik süresi boyunca (±drift) TEKRAR kullanılamamalıdır (replay attack):
        // kodu omuz üstünden gören biri aynı 30 sn içinde onu kullanamasın.
        // Bu kullanıcı için bu zaman dilimi ilk kez kullanılıyorsa true, daha önce kullanıldıysa false döner.
        Task<bool> TryConsumeTimeStepAsync(Guid userId, long timeStep, CancellationToken cancellationToken = default);
    }
}
