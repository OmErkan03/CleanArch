using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Uludag.Core.Caching.Redis;

namespace Uludag.Core.RateLimiting
{
    // LEAKY BUCKET (sızdıran kova) algoritması, "sayaç" (meter) yorumu:
    //
    //   - Her kullanıcı/IP'nin bir kovası vardır, kapasitesi Capacity (10) damladır.
    //   - Her istek kovaya 1 damla ekler. Kova doluysa istek REDDEDİLİR (429).
    //   - Kovanın dibindeki delikten her LeakIntervalSeconds (12 sn) saniyede bir damla akar.
    //
    //   Sonuç: boş kovayla 10 istek arka arkaya geçebilir; sonrasında ancak 12 sn'de bir istek kabul edilir.
    //   Kullanıcı bir süre istek atmazsa kova boşalır ve tekrar 10 isteklik hak birikir.
    //
    // Neden Redis + Lua?
    //   - API birden fazla instance'ta çalışsa bile hepsi AYNI kovayı görür.
    //   - "Oku => hesapla => yaz" adımları tek bir Lua script'inde çalışır; Redis script'leri atomik yürüttüğü için
    //     aynı anda gelen iki istek kovayı aynı anda okuyup ikisi birden "yer var" diyemez (race condition yok).
    //   - Zaman, uygulama sunucularının değil Redis'in saatinden (TIME) alınır; sunucular arası saat farkı sonucu bozmaz.
    //
    // Redis'te kova: hash { level = kovadaki damla sayısı, ts = son damlanın aktığı (ya da sayacın başladığı) an (ms) }.
    // Kova tamamen boşalınca anahtar TTL ile kendiliğinden silinir; hareketsiz kullanıcılar Redis'te yer kaplamaz.
    public class RedisLeakyBucketRateLimiter : IRateLimiter
    {
        private const string KeyPrefix = "rate-limit:";

        // KEYS[1] = kova anahtarı, ARGV[1] = kapasite, ARGV[2] = damlama aralığı (ms)
        // Dönüş: { izin (1/0), kovadaki damla sayısı, yeniden deneme süresi (ms) }
        private const string LeakyBucketScript = """
            local key = KEYS[1]
            local capacity = tonumber(ARGV[1])
            local interval = tonumber(ARGV[2])

            local time = redis.call('TIME')
            local now = tonumber(time[1]) * 1000 + math.floor(tonumber(time[2]) / 1000)

            local state = redis.call('HMGET', key, 'level', 'ts')
            local level = tonumber(state[1]) or 0
            local ts = tonumber(state[2]) or now

            -- Son hesaptan bu yana kaç TAM damla aktı? (12 sn dolmadan damla akmaz)
            local leaked = math.floor((now - ts) / interval)
            if leaked > 0 then
                level = math.max(0, level - leaked)
                ts = ts + leaked * interval
            end

            -- Boş kovada damlama saati, gelen ilk damlayla yeniden başlar.
            if level == 0 then
                ts = now
            end

            local allowed = 0
            local retryAfter = 0
            if level < capacity then
                level = level + 1
                allowed = 1
            else
                -- Kova dolu: bir sonraki damlanın akmasına kalan süre
                retryAfter = interval - (now - ts)
            end

            redis.call('HSET', key, 'level', level, 'ts', ts)
            -- Kovanın tamamen boşalması en fazla level * interval sürer; o zamana kadar anahtar yaşasın.
            redis.call('PEXPIRE', key, level * interval + interval)

            return { allowed, level, retryAfter }
            """;

        private readonly IConnectionMultiplexer _connection;
        private readonly LeakyBucketOptions _options;
        private readonly string _instanceName;
        private readonly ILogger<RedisLeakyBucketRateLimiter> _logger;

        public RedisLeakyBucketRateLimiter(
            IConnectionMultiplexer connection,
            LeakyBucketOptions options,
            RedisOptions redisOptions,
            ILogger<RedisLeakyBucketRateLimiter> logger)
        {
            if (options.Capacity <= 0)
                throw new InvalidOperationException($"'{LeakyBucketOptions.SectionName}:Capacity' 0'dan büyük olmalıdır.");
            if (options.LeakIntervalSeconds <= 0)
                throw new InvalidOperationException($"'{LeakyBucketOptions.SectionName}:LeakIntervalSeconds' 0'dan büyük olmalıdır.");

            _connection = connection;
            _options = options;
            _instanceName = redisOptions.InstanceName;
            _logger = logger;
        }

        public async Task<RateLimitResult> TryAcquireAsync(string partitionKey, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = (RedisResult[])(await _connection.GetDatabase().ScriptEvaluateAsync(
                    LeakyBucketScript,
                    [(RedisKey)(_instanceName + KeyPrefix + partitionKey)],
                    [_options.Capacity, _options.LeakIntervalSeconds * 1000]))!;

                var isAllowed = (int)result[0] == 1;
                var level = (int)result[1];
                var retryAfter = TimeSpan.FromMilliseconds((long)result[2]);

                return new RateLimitResult(isAllowed, _options.Capacity, _options.Capacity - level, retryAfter);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException)
            {
                // FAIL-OPEN: Redis'e ulaşılamıyorsa rate limiting tek başına sistemi durdurmasın; istek geçer, durum loglanır.
                // (RedisConnectionException ve RedisTimeoutException, RedisException'dan türer.)
                _logger.LogWarning(ex, "Rate limiter Redis'e ulaşamadı; '{PartitionKey}' için istek sınırlanmadan geçirildi.", partitionKey);
                return RateLimitResult.Unavailable(_options.Capacity);
            }
        }
    }
}
