namespace Uludag.Core.Security.MultiFactor
{
    public enum MfaChallengePurpose
    {
        // Kullanıcının MFA'sı yok => önce authenticator kurmalı.
        Setup,

        // Kullanıcının MFA'sı var => authenticator'daki kodu girmeli.
        Verify
    }

    // Şifresi doğrulanmış ama MFA adımını henüz tamamlamamış kullanıcının "yarım kalmış girişi".
    // Redis'te kısa süreli (MfaOptions.ChallengeExpirationMinutes) tutulur. İstemciye yalnızca anahtarı (MFA token) verilir;
    // bu anahtar JWT DEĞİLDİR, korumalı endpoint'lere erişim sağlamaz, yalnızca MFA endpoint'lerinde geçerlidir.
    public class MfaChallenge
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public MfaChallengePurpose Purpose { get; set; }

        // Kurulum sırasında üretilen, henüz onaylanmamış gizli anahtar (ŞİFRELİ).
        // Kullanıcı ilk doğru kodu girene kadar veritabanına yazılmaz.
        public string? PendingEncryptedSecret { get; set; }

        public int FailedAttempts { get; set; }

        // UTC
        public DateTime ExpiresAt { get; set; }
    }
}
