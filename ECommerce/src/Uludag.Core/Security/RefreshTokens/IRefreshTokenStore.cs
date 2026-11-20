namespace Uludag.Core.Security.RefreshTokens
{
    // Refresh token'ları saklar ve döndürür (rotation).
    //
    // AİLE (family) kavramı: bir login'den doğan tüm refresh token'lar aynı ailedir. Her yenilemede ailenin token'ı
    // değişir (eskisi geçersiz olur) ama ailenin bitiş zamanı (login + 3 gün) DEĞİŞMEZ. Ailede her an yalnızca
    // tek bir geçerli token vardır.
    //
    // Token biçimi: "{familyId}.{secret}". Redis'te secret'ın kendisi değil SHA-256 özeti saklanır;
    // Redis verisi okunsa bile geçerli bir token üretilemez (şifrelerin hash'lenmesiyle aynı mantık).
    public interface IRefreshTokenStore
    {
        // Yeni bir aile (login) başlatır ve ilk refresh token'ı döndürür.
        Task<IssuedRefreshToken> CreateAsync(Guid userId, string sessionKey, DateTime expiresAt, CancellationToken cancellationToken = default);

        // Token'ı tek kullanımlık olarak tüketir ve yerine yenisini verir (atomik).
        //   Rotated  : token geçerliydi; NewToken ailenin yeni token'ıdır.
        //   Reused   : ailenin ESKİ bir token'ı kullanıldı => token çalınmış olabilir; aile iptal edildi.
        //   NotFound : token bozuk, süresi dolmuş ya da aile iptal edilmiş.
        Task<RefreshTokenRotation> RotateAsync(string refreshToken, CancellationToken cancellationToken = default);

        // Yenileme sonrası ailenin işaret ettiği access oturumunu günceller (aile bu arada iptal edildiyse bir şey yapmaz).
        Task SetSessionKeyAsync(string familyId, string sessionKey, CancellationToken cancellationToken = default);

        // Aileyi iptal eder (logout). Ailedeki hiçbir token artık kullanılamaz.
        Task RevokeAsync(string familyId, CancellationToken cancellationToken = default);

        // Kullanıcının tüm ailelerini iptal eder (tüm cihazlardan çıkış, MFA sıfırlama).
        Task<int> RevokeUserFamiliesAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    public sealed record IssuedRefreshToken(string Token, string FamilyId, DateTime ExpiresAt);

    public enum RefreshTokenRotationStatus
    {
        Rotated,
        Reused,
        NotFound
    }

    public sealed record RefreshTokenRotation(
        RefreshTokenRotationStatus Status,
        Guid UserId = default,
        string? FamilyId = null,
        // Ailenin yenilemeden ÖNCE işaret ettiği access oturumu (kapatılmalı)
        string? PreviousSessionKey = null,
        string? NewToken = null,
        // Ailenin mutlak bitiş zamanı (UTC)
        DateTime ExpiresAt = default)
    {
        public static RefreshTokenRotation NotFound { get; } = new(RefreshTokenRotationStatus.NotFound);
    }
}
