using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;
using Uludag.Core.Caching.Redis;

namespace Uludag.Core.Security.RefreshTokens
{
    // IRefreshTokenStore'un Redis implementasyonu.
    //
    // Redis'teki anahtarlar ({InstanceName} ön ekiyle):
    //   refresh-family:{familyId}          => hash { userId, tokenHash, sessionKey, expiresAt }
    //                                         TTL = ailenin mutlak bitiş zamanı (PEXPIREAT) => 3 gün sonra kendiliğinden silinir
    //   user-refresh-families:{userId}     => set, kullanıcının aileleri (logout-all / MFA sıfırlama için index)
    //
    // Rotation neden Lua script'i?
    //   "Token doğru mu? => yenisini yaz" iki ayrı komut olsaydı, aynı token'la aynı anda gelen iki istek ikisi birden
    //   "doğru" görüp iki farklı yeni token alabilirdi. Script atomik çalıştığı için yalnızca biri kazanır;
    //   diğeri artık eski token'ı kullanmış olur ve "Reused" sonucu alır.
    public class RedisRefreshTokenStore : IRefreshTokenStore
    {
        private const string FamilyKeyPrefix = "refresh-family:";
        private const string UserFamiliesKeyPrefix = "user-refresh-families:";

        // KEYS[1] = aile, ARGV[1] = sunulan token'ın özeti, ARGV[2] = yeni token'ın özeti
        // Dönüş: {0} bulunamadı | {-1, userId, sessionKey} yeniden kullanım => aile silindi | {1, userId, sessionKey, expiresAt} döndürüldü
        // Not: Özetler (SHA-256) karşılaştırılır; özetin zamanlama ile sızması token'ı tahmin etmeye yaramaz.
        private const string RotateScript = """
            local f = redis.call('HMGET', KEYS[1], 'tokenHash', 'userId', 'sessionKey', 'expiresAt')
            if not f[1] then
                return { 0 }
            end
            if f[1] ~= ARGV[1] then
                redis.call('DEL', KEYS[1])
                return { -1, f[2], f[3] }
            end
            redis.call('HSET', KEYS[1], 'tokenHash', ARGV[2])
            return { 1, f[2], f[3], f[4] }
            """;

        // Aile varsa günceller; yoksa (iptal/süre doldu) TTL'siz yeni bir anahtar oluşmasın diye hiçbir şey yapmaz.
        private const string SetSessionKeyScript = """
            if redis.call('EXISTS', KEYS[1]) == 1 then
                redis.call('HSET', KEYS[1], 'sessionKey', ARGV[1])
            end
            return 0
            """;

        private readonly IConnectionMultiplexer _connection;
        private readonly string _instanceName;

        public RedisRefreshTokenStore(IConnectionMultiplexer connection, RedisOptions redisOptions)
        {
            _connection = connection;
            _instanceName = redisOptions.InstanceName;
        }

        private IDatabase Database => _connection.GetDatabase();
        private RedisKey FamilyKey(string familyId) => _instanceName + FamilyKeyPrefix + familyId;
        private RedisKey UserFamiliesKey(Guid userId) => _instanceName + UserFamiliesKeyPrefix + userId;

        public async Task<IssuedRefreshToken> CreateAsync(Guid userId, string sessionKey, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            if (expiresAt <= DateTime.UtcNow)
                throw new InvalidOperationException("Refresh token'ın bitiş zamanı geçmişte olamaz.");

            var familyId = RandomHex(16);
            var secret = RandomHex(32);
            var familyKey = FamilyKey(familyId);

            await Database.HashSetAsync(familyKey,
            [
                new HashEntry("userId", userId.ToString()),
                new HashEntry("tokenHash", Hash(secret)),
                new HashEntry("sessionKey", sessionKey),
                new HashEntry("expiresAt", new DateTimeOffset(expiresAt, TimeSpan.Zero).ToUnixTimeMilliseconds())
            ]);
            // Mutlak bitiş: TTL "şu andan itibaren" değil, belirli bir ANA göre ayarlanır.
            await Database.KeyExpireAsync(familyKey, expiresAt);

            // Index: kullanıcının en geç biten ailesi kadar yaşar (süreyi asla kısaltma: NX + GT).
            var userFamiliesKey = UserFamiliesKey(userId);
            await Database.SetAddAsync(userFamiliesKey, familyId);
            await Database.KeyExpireAsync(userFamiliesKey, expiresAt, ExpireWhen.HasNoExpiry);
            await Database.KeyExpireAsync(userFamiliesKey, expiresAt, ExpireWhen.GreaterThanCurrentExpiry);

            return new IssuedRefreshToken($"{familyId}.{secret}", familyId, expiresAt);
        }

        public async Task<RefreshTokenRotation> RotateAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (!TryParse(refreshToken, out var familyId, out var secret))
                return RefreshTokenRotation.NotFound;

            var newSecret = RandomHex(32);
            var result = (RedisResult[])(await Database.ScriptEvaluateAsync(
                RotateScript, [FamilyKey(familyId)], [Hash(secret), Hash(newSecret)]))!;

            var status = (int)result[0];
            if (status == 0)
                return RefreshTokenRotation.NotFound;

            var userId = Guid.Parse((string)result[1]!);
            var previousSessionKey = (string?)result[2];

            if (status == -1)
            {
                await Database.SetRemoveAsync(UserFamiliesKey(userId), familyId);
                return new RefreshTokenRotation(RefreshTokenRotationStatus.Reused, userId, familyId, previousSessionKey);
            }

            var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds((long)result[3]).UtcDateTime;
            return new RefreshTokenRotation(RefreshTokenRotationStatus.Rotated, userId, familyId, previousSessionKey,
                $"{familyId}.{newSecret}", expiresAt);
        }

        public Task SetSessionKeyAsync(string familyId, string sessionKey, CancellationToken cancellationToken = default) =>
            Database.ScriptEvaluateAsync(SetSessionKeyScript, [FamilyKey(familyId)], [sessionKey]);

        public async Task RevokeAsync(string familyId, CancellationToken cancellationToken = default)
        {
            var familyKey = FamilyKey(familyId);
            var userId = await Database.HashGetAsync(familyKey, "userId");
            await Database.KeyDeleteAsync(familyKey);

            if (!userId.IsNullOrEmpty)
                await Database.SetRemoveAsync(UserFamiliesKey(Guid.Parse(userId.ToString())), familyId);
        }

        public async Task<int> RevokeUserFamiliesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userFamiliesKey = UserFamiliesKey(userId);
            var familyIds = await Database.SetMembersAsync(userFamiliesKey);

            var revoked = familyIds.Length == 0
                ? 0
                : (int)await Database.KeyDeleteAsync(familyIds.Select(id => FamilyKey(id.ToString())).ToArray());

            await Database.KeyDeleteAsync(userFamiliesKey);
            return revoked;
        }

        private static bool TryParse(string? token, out string familyId, out string secret)
        {
            familyId = secret = string.Empty;
            var parts = token?.Split('.');
            if (parts is not [var f, var s] || f.Length != 32 || s.Length != 64 || !IsHex(f) || !IsHex(s))
                return false;

            (familyId, secret) = (f, s);
            return true;
        }

        private static bool IsHex(string value) => value.All(char.IsAsciiHexDigitLower);

        private static string RandomHex(int byteCount) =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(byteCount)).ToLowerInvariant();

        private static string Hash(string secret) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }
}
