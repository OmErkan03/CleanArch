using System.Security.Cryptography;
using Uludag.Core.Caching;

namespace Uludag.Core.Security.Sessions
{
    // ISessionStore'un ICacheService (Redis) üzerinde çalışan implementasyonu.
    //
    // Redis'teki anahtarlar ({InstanceName} ön eki RedisCacheService tarafından eklenir):
    //   session:{sessionKey}       => string, UserSession'ın JSON hali. TTL = token ömrü.
    //   user-sessions:{userId}     => set, kullanıcının oturum anahtarları.
    //
    // İkinci anahtar bir "index"tir: JWT'de yalnızca oturum anahtarı olduğu için, rolü değişen bir kullanıcının
    // hangi oturumlarının güncelleneceğini başka türlü bulamayız (tüm Redis'i taramak - KEYS/SCAN - pahalıdır).
    public class CacheSessionStore : ISessionStore
    {
        private const string SessionKeyPrefix = "session:";
        private const string UserSessionsKeyPrefix = "user-sessions:";

        private readonly ICacheService _cacheService;

        public CacheSessionStore(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        private static string SessionCacheKey(string sessionKey) => SessionKeyPrefix + sessionKey;
        private static string UserSessionsCacheKey(Guid userId) => UserSessionsKeyPrefix + userId;

        public async Task<string> CreateAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            // Tahmin edilemez anahtar: 256 bit kriptografik rastgele değer.
            var sessionKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

            // TTL token ömrüyle aynıdır => süresi dolan oturum Redis'ten kendiliğinden silinir.
            var ttl = session.ExpiresAt - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
                throw new InvalidOperationException("Oturumun bitiş zamanı geçmişte olamaz.");

            await _cacheService.SetAsync(SessionCacheKey(sessionKey), session, ttl, cancellationToken);

            // Index'e ekle. Set'in süresi, içindeki en uzun ömürlü oturum kadar olmalıdır;
            // böylece kullanıcının tüm oturumları bitince set de kendiliğinden silinir (Redis'te çöp birikmez).
            var userSessionsKey = UserSessionsCacheKey(session.UserId);
            await _cacheService.SetAddAsync(userSessionsKey, sessionKey, cancellationToken);
            await _cacheService.ExtendExpirationAsync(userSessionsKey, ttl, cancellationToken);

            return sessionKey;
        }

        public Task<UserSession?> GetAsync(string sessionKey, CancellationToken cancellationToken = default) =>
            _cacheService.GetAsync<UserSession>(SessionCacheKey(sessionKey), cancellationToken);

        public async Task RemoveAsync(string sessionKey, CancellationToken cancellationToken = default)
        {
            var session = await GetAsync(sessionKey, cancellationToken);
            await _cacheService.RemoveAsync(SessionCacheKey(sessionKey), cancellationToken);

            if (session is not null)
                await _cacheService.SetRemoveAsync(UserSessionsCacheKey(session.UserId), sessionKey);
        }

        public async Task<int> UpdateUserSessionsAsync(Guid userId, Action<UserSession> update, CancellationToken cancellationToken = default)
        {
            var userSessionsKey = UserSessionsCacheKey(userId);
            var sessionKeys = await _cacheService.SetMembersAsync(userSessionsKey, cancellationToken);

            var updatedCount = 0;
            List<string> expiredKeys = [];

            foreach (var sessionKey in sessionKeys)
            {
                var session = await GetAsync(sessionKey, cancellationToken);
                if (session is null)
                {
                    // Oturumun süresi dolmuş ama anahtarı index'te kalmış => temizlenir.
                    expiredKeys.Add(sessionKey);
                    continue;
                }

                update(session);

                // KEEPTTL + XX: kalan süre korunur; oturum bu arada sona erdiyse (logout / TTL) geri yazılmaz.
                if (await _cacheService.ReplaceAsync(SessionCacheKey(sessionKey), session, cancellationToken))
                    updatedCount++;
                else
                    expiredKeys.Add(sessionKey);
            }

            if (expiredKeys.Count > 0)
                await _cacheService.SetRemoveAsync(userSessionsKey, [.. expiredKeys]);

            return updatedCount;
        }
    }
}
