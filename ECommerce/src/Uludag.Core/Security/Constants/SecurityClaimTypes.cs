namespace Uludag.Core.Security.Constants
{
    // Claim tiplerinin adları.
    // JwtHelper (yazan), SessionTokenValidator (Redis'ten doldurduğu claim'ler), CurrentUser (okuyan)
    // ve Program.cs (RoleClaimType) aynı sabitleri kullanır.
    public static class SecurityClaimTypes
    {
        // JWT'nin taşıdığı TEK kullanıcı claim'i: Redis'teki oturumun anahtarı.
        public const string SessionKey = "sid";

        // Kullanıcının her operation claim'i, Redis'teki oturumdan okunup HttpContext.User'a ayrı bir "role" claim'i olarak eklenir.
        public const string OperationClaim = "role";
    }
}
