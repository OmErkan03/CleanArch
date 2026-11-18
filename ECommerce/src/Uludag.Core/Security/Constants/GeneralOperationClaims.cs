namespace Uludag.Core.Security.Constants
{
    // Belirli bir entity'ye bağlı olmayan, sistem geneli operation claim'ler.
    public static class GeneralOperationClaims
    {
        // Sahibi tüm işlemleri yapabilir (AuthorizationBehavior bu claim'i her istekte kabul eder).
        public const string Admin = "Admin";
    }
}
