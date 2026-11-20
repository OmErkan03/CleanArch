namespace Uludag.Core.Security.JWT
{
    // appsettings'teki "TokenOptions" bölümünün karşılığı.
    // Hem token üretilirken (JwtHelper) hem de gelen token doğrulanırken (Program.cs => AddJwtBearer) aynı değerler kullanılır.
    public class TokenOptions
    {
        public const string SectionName = "TokenOptions";

        // Token'ı kim üretti? (bu API)
        public string Issuer { get; set; } = string.Empty;

        // Token kimin için üretildi? (bu API'yi kullanacak istemciler)
        public string Audience { get; set; } = string.Empty;

        // İmzalama anahtarı. HMAC-SHA256 için en az 32 byte (32 karakter) olmalıdır.
        // Bu anahtarı bilen herkes geçerli token üretebilir => repoya değil secret store / ortam değişkenine konmalıdır.
        public string SecurityKey { get; set; } = string.Empty;

        public int AccessTokenExpirationMinutes { get; set; } = 60;

        // Refresh token ailesinin MUTLAK ömrü: login anından itibaren sayılır, yenilemeler uzatmaz.
        // Bu süre dolunca kullanıcı yeniden şifre + MFA ile giriş yapmalıdır.
        public int RefreshTokenExpirationDays { get; set; } = 3;
    }
}
