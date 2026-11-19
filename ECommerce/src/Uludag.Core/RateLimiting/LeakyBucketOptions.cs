namespace Uludag.Core.RateLimiting
{
    // appsettings'teki "LeakyBucketOptions" bölümünün karşılığı.
    public class LeakyBucketOptions
    {
        public const string SectionName = "LeakyBucketOptions";

        // Kovaya en fazla kaç istek (damla) sığar. Boş kovayla arka arkaya bu kadar istek hemen geçebilir (burst).
        public int Capacity { get; set; } = 10;

        // Kovadan kaç saniyede bir damla akar. Kova doluyken yeni istek ancak bir damla akınca kabul edilir.
        // Uzun vadede izin verilen hız = 1 istek / LeakIntervalSeconds (12 sn => dakikada 5 istek).
        public int LeakIntervalSeconds { get; set; } = 12;

        // Rate limiting dışında tutulan yol önekleri (ör. "/swagger", "/openapi"). Büyük/küçük harf duyarsız.
        public List<string> ExcludedPaths { get; set; } = [];
    }
}
