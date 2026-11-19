using OtpNet;
using Uludag.Core.Security.Encryption;

namespace Uludag.Core.Security.MultiFactor
{
    // ITotpHelper'ın OtpNet kütüphanesiyle yazılmış implementasyonu.
    public class TotpHelper : ITotpHelper
    {
        private const int SecretSizeInBytes = 20; // 160 bit
        private const int CodeLength = 6;

        private readonly MfaOptions _options;

        public TotpHelper(MfaOptions options)
        {
            _options = options;

            if (string.IsNullOrWhiteSpace(options.Issuer))
                throw new InvalidOperationException($"'{MfaOptions.SectionName}:Issuer' boş olamaz.");

            if (string.IsNullOrWhiteSpace(options.SecretEncryptionKey)
                || Convert.FromBase64String(options.SecretEncryptionKey).Length != 32)
                throw new InvalidOperationException($"'{MfaOptions.SectionName}:SecretEncryptionKey' Base64 formatında 32 byte olmalıdır.");
        }

        public string GenerateSecret() =>
            // KeyGeneration kriptografik rastgele sayı üreteci (RandomNumberGenerator) kullanır.
            Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(SecretSizeInBytes));

        public string GenerateProvisioningUri(string secret, string accountName) =>
            new OtpUri(OtpType.Totp, secret, accountName, _options.Issuer, digits: CodeLength).ToString();

        public bool VerifyCode(string secret, string code, out long timeStep)
        {
            timeStep = 0;

            // Biçim kontrolü: 6 rakam değilse HMAC hesaplamaya gerek yok.
            code = code?.Trim() ?? string.Empty;
            if (code.Length != CodeLength || !code.All(char.IsAsciiDigit))
                return false;

            var totp = new Totp(Base32Encoding.ToBytes(secret), step: 30, totpSize: CodeLength);

            // VerificationWindow(geri, ileri): saat farkı toleransı. Karşılaştırma sabit sürelidir (timing attack'a karşı).
            var window = new VerificationWindow(previous: _options.AllowedTimeStepDrift, future: _options.AllowedTimeStepDrift);
            return totp.VerifyTotp(code, out timeStep, window);
        }

        public string EncryptSecret(string secret) =>
            EncryptionHelper.EncryptSymmetric(secret, _options.SecretEncryptionKey);

        public string DecryptSecret(string encryptedSecret) =>
            EncryptionHelper.DecryptSymmetric(encryptedSecret, _options.SecretEncryptionKey);
    }
}
