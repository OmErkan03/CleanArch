namespace Uludag.Core.Security.Sessions
{
    // Oturumun Redis'te saklanan hali.
    // JWT yalnızca bu kaydın anahtarını taşır; kullanıcı bilgileri (email, yetkiler...) her istekte buradan okunur.
    public class UserSession
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<string> OperationClaims { get; set; } = [];

        // Bu oturumu açan refresh token ailesi (bkz. IRefreshTokenStore). Logout, oturumla birlikte bu aileyi de iptal eder.
        public string? RefreshTokenFamilyId { get; set; }

        // UTC
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
