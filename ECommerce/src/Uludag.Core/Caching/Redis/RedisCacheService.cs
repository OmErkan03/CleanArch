using System.Text.Json;
using StackExchange.Redis;

namespace Uludag.Core.Caching.Redis
{
    // ICacheService'in Redis implementasyonu.
    // Değerler System.Text.Json ile string'e çevrilip Redis'te "string" tipinde saklanır.
    public class RedisCacheService : ICacheService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IConnectionMultiplexer _connection;
        private readonly string _instanceName;

        public RedisCacheService(IConnectionMultiplexer connection, RedisOptions options)
        {
            _connection = connection;
            _instanceName = options.InstanceName;
        }

        // IDatabase hafif bir nesnedir; her çağrıda istemek önerilen kullanımdır (bağlantıyı multiplexer tutar).
        private IDatabase Database => _connection.GetDatabase();

        private RedisKey BuildKey(string key) => _instanceName + key;

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var value = await Database.StringGetAsync(BuildKey(key));
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            return expiration.HasValue
                ? Database.StringSetAsync(BuildKey(key), json, expiration.Value)
                : Database.StringSetAsync(BuildKey(key), json);
        }

        public Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);

            // SET key value KEEPTTL XX
            //   KEEPTTL => kalan süre sıfırlanmaz (oturum, token'la aynı anda bitmeye devam eder)
            //   XX      => yalnızca anahtar varsa yazar (tek komut olduğu için "var mı? => yaz" arasında yarış olmaz)
            return Database.StringSetAsync(BuildKey(key), json, expiry: null, keepTtl: true, when: When.Exists);
        }

        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default) =>
            Database.KeyDeleteAsync(BuildKey(key));

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
            Database.KeyExistsAsync(BuildKey(key));

        public Task SetAddAsync(string key, string member, CancellationToken cancellationToken = default) =>
            Database.SetAddAsync(BuildKey(key), member);

        public async Task<string[]> SetMembersAsync(string key, CancellationToken cancellationToken = default)
        {
            var members = await Database.SetMembersAsync(BuildKey(key));
            return members.Select(m => m.ToString()).ToArray();
        }

        public Task SetRemoveAsync(string key, params string[] members) =>
            Database.SetRemoveAsync(BuildKey(key), members.Select(m => (RedisValue)m).ToArray());

        public async Task ExtendExpirationAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            var redisKey = BuildKey(key);

            // EXPIRE ... NX => anahtarın hiç süresi yoksa ayarla (yeni oluşturulmuş set)
            // EXPIRE ... GT => mevcut süre daha kısaysa uzat
            // İkisi birlikte "süreyi asla kısaltma" anlamına gelir (Redis 7+).
            await Database.KeyExpireAsync(redisKey, expiration, ExpireWhen.HasNoExpiry);
            await Database.KeyExpireAsync(redisKey, expiration, ExpireWhen.GreaterThanCurrentExpiry);
        }
    }
}
