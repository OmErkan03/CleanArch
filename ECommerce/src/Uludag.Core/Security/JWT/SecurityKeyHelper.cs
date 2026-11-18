using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Uludag.Core.Security.JWT
{
    // Token'ı İMZALAYAN (JwtHelper) ve DOĞRULAYAN (AddJwtBearer) taraf aynı anahtarı kullanmak zorundadır.
    // Anahtar tek yerden üretilsin diye bu yardımcı kullanılır.
    public static class SecurityKeyHelper
    {
        private const int MinimumKeyLength = 32; // HMAC-SHA256 için 256 bit

        // Simetrik anahtar: imzalayan da doğrulayan da aynı anahtarı bilir (bkz. EncryptionHelper'daki simetrik/asimetrik farkı).
        public static SecurityKey CreateSecurityKey(string securityKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(securityKey);
            if (keyBytes.Length < MinimumKeyLength)
                throw new InvalidOperationException($"TokenOptions:SecurityKey en az {MinimumKeyLength} byte olmalıdır.");

            return new SymmetricSecurityKey(keyBytes);
        }

        // HashingHelper'daki HMAC mantığının aynısı: token içeriği anahtarla HMAC'lenir.
        // İçerik değiştirilirse imza tutmaz ve token reddedilir.
        public static SigningCredentials CreateSigningCredentials(SecurityKey securityKey) =>
            new(securityKey, SecurityAlgorithms.HmacSha256);
    }
}
