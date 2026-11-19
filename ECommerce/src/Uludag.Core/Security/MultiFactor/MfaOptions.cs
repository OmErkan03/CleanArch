namespace Uludag.Core.Security.MultiFactor
{
    // appsettings'teki "MfaOptions" bölümünün karşılığı.
    public class MfaOptions
    {
        public const string SectionName = "MfaOptions";

        // Authenticator uygulamasında (Google/Microsoft Authenticator...) hesabın üstünde görünen ad.
        public string Issuer { get; set; } = string.Empty;

        // Kullanıcıların TOTP gizli anahtarları veritabanında bu anahtarla AES-GCM ile ŞİFRELİ saklanır.
        // Base64, 32 byte (EncryptionHelper.CreateSymmetricKeyBase64() ile üretilebilir).
        // Veritabanı sızsa bile bu anahtar olmadan kimse kullanıcıların kodlarını üretemez => repoya değil secret store'a konmalıdır.
        public string SecretEncryptionKey { get; set; } = string.Empty;

        // Login sonrası verilen geçici MFA token'ının ömrü. Bu sürede kurulum/doğrulama tamamlanmalıdır.
        public int ChallengeExpirationMinutes { get; set; } = 5;

        // Bir MFA token'ı ile en fazla kaç kez yanlış kod denenebilir. Aşılırsa token iptal edilir, yeniden login gerekir.
        // 6 haneli kodda 1.000.000 olasılık vardır; deneme sınırı olmazsa kaba kuvvetle (brute force) bulunabilir.
        public int MaxFailedAttempts { get; set; } = 5;

        // Telefon ile sunucu saati arasındaki farka tolerans: her adım 30 sn. 1 => önceki/sonraki kod da kabul edilir (±30 sn).
        public int AllowedTimeStepDrift { get; set; } = 1;
    }
}
