namespace Uludag.Core.Security.MultiFactor
{
    // TOTP (Time-based One-Time Password, RFC 6238) işlemleri.
    //
    // Nasıl çalışır?
    //   1) Sunucu rastgele bir GİZLİ ANAHTAR üretir ve kullanıcıya QR kod (otpauth:// adresi) olarak gösterir.
    //   2) Kullanıcı QR'ı authenticator uygulamasıyla okutur; artık anahtar hem sunucuda hem telefonda vardır.
    //   3) Her 30 saniyede bir iki taraf da HMAC(gizli anahtar, şu anki 30 sn'lik dilim) ile aynı 6 haneli kodu üretir.
    //      İnternet bağlantısı gerekmez; ortak olan tek şey anahtar ve saattir.
    //   4) Girişte kullanıcı telefondaki kodu yazar, sunucu kendi ürettiği kodla karşılaştırır.
    public interface ITotpHelper
    {
        // Base32 gizli anahtar (authenticator uygulamalarının beklediği format). 160 bit (RFC 4226 önerisi).
        string GenerateSecret();

        // QR koda dönüştürülecek adres: otpauth://totp/{Issuer}:{accountName}?secret=...&issuer=...
        string GenerateProvisioningUri(string secret, string accountName);

        // Kod doğruysa true döner. timeStep: kodun ait olduğu 30 sn'lik dilim; aynı kodun tekrar kullanımını engellemek için gerekir.
        bool VerifyCode(string secret, string code, out long timeStep);

        // Gizli anahtar veritabanına / Redis'e ŞİFRELİ yazılır, kullanılacağı an çözülür (bkz. MfaOptions.SecretEncryptionKey).
        string EncryptSecret(string secret);
        string DecryptSecret(string encryptedSecret);
    }
}
