namespace Uludag.Core.RateLimiting
{
    // Bir isteğin kabul edilip edilmeyeceğine karar verir.
    // Core HTTP'yi tanımaz: kovanın kime ait olduğunu (kullanıcı, IP...) çağıran taraf partitionKey olarak verir.
    public interface IRateLimiter
    {
        Task<RateLimitResult> TryAcquireAsync(string partitionKey, CancellationToken cancellationToken = default);
    }

    public sealed record RateLimitResult(
        bool IsAllowed,
        // Kova kapasitesi
        int Limit,
        // Kovada kalan boş yer (bu istekten sonra arka arkaya kaç istek daha hemen geçebilir)
        int Remaining,
        // Reddedildiyse bir sonraki damlanın akmasına (yer açılmasına) kalan süre; kabul edildiyse sıfır
        TimeSpan RetryAfter)
    {
        // Rate limiter karar veremediğinde (Redis'e ulaşılamadı) isteğe izin verilir (fail-open).
        public static RateLimitResult Unavailable(int limit) => new(true, limit, limit, TimeSpan.Zero);
    }
}
