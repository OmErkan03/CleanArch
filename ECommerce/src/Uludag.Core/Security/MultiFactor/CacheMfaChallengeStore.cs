using System.Security.Cryptography;
using Uludag.Core.Caching;

namespace Uludag.Core.Security.MultiFactor
{
    // IMfaChallengeStore'un ICacheService (Redis) üzerinde çalışan implementasyonu.
    //
    // Redis'teki anahtarlar:
    //   mfa-challenge:{mfaToken}          => MfaChallenge JSON'ı. TTL = MfaOptions.ChallengeExpirationMinutes
    //   mfa-used:{userId}:{timeStep}      => kullanılmış kod işareti. TTL = kodun geçerli kalabileceği süre
    //   user-mfa-challenges:{userId}      => set, kullanıcının bekleyen MFA token'ları (logout'ta toplu iptal için index)
    public class CacheMfaChallengeStore : IMfaChallengeStore
    {
        private const string ChallengeKeyPrefix = "mfa-challenge:";
        private const string UsedTimeStepKeyPrefix = "mfa-used:";
        private const string UserChallengesKeyPrefix = "user-mfa-challenges:";
        private const int TimeStepSeconds = 30;

        private readonly ICacheService _cacheService;
        private readonly MfaOptions _options;

        public CacheMfaChallengeStore(ICacheService cacheService, MfaOptions options)
        {
            _cacheService = cacheService;
            _options = options;
        }

        private static string ChallengeCacheKey(string mfaToken) => ChallengeKeyPrefix + mfaToken;
        private static string UserChallengesCacheKey(Guid userId) => UserChallengesKeyPrefix + userId;

        public async Task<string> CreateAsync(MfaChallenge challenge, CancellationToken cancellationToken = default)
        {
            var mfaToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

            var ttl = challenge.ExpiresAt - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
                throw new InvalidOperationException("MFA challenge'ın bitiş zamanı geçmişte olamaz.");

            await _cacheService.SetAsync(ChallengeCacheKey(mfaToken), challenge, ttl, cancellationToken);

            // Index: kullanıcının bekleyen MFA token'ları (logout'ta hepsi iptal edilir). En geç biten challenge kadar yaşar.
            var userChallengesKey = UserChallengesCacheKey(challenge.UserId);
            await _cacheService.SetAddAsync(userChallengesKey, mfaToken, cancellationToken);
            await _cacheService.ExtendExpirationAsync(userChallengesKey, ttl, cancellationToken);

            return mfaToken;
        }

        public Task<MfaChallenge?> GetAsync(string mfaToken, CancellationToken cancellationToken = default) =>
            string.IsNullOrWhiteSpace(mfaToken)
                ? Task.FromResult<MfaChallenge?>(null)
                : _cacheService.GetAsync<MfaChallenge>(ChallengeCacheKey(mfaToken), cancellationToken);

        public Task<bool> UpdateAsync(string mfaToken, MfaChallenge challenge, CancellationToken cancellationToken = default) =>
            _cacheService.ReplaceAsync(ChallengeCacheKey(mfaToken), challenge, cancellationToken);

        public async Task RemoveAsync(string mfaToken, CancellationToken cancellationToken = default)
        {
            var challenge = await GetAsync(mfaToken, cancellationToken);
            await _cacheService.RemoveAsync(ChallengeCacheKey(mfaToken), cancellationToken);

            if (challenge is not null)
                await _cacheService.SetRemoveAsync(UserChallengesCacheKey(challenge.UserId), mfaToken);
        }

        public async Task<int> RemoveUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var userChallengesKey = UserChallengesCacheKey(userId);
            var mfaTokens = await _cacheService.SetMembersAsync(userChallengesKey, cancellationToken);

            var removed = 0;
            foreach (var mfaToken in mfaTokens)
            {
                if (await _cacheService.RemoveAsync(ChallengeCacheKey(mfaToken), cancellationToken))
                    removed++;
            }

            await _cacheService.RemoveAsync(userChallengesKey, cancellationToken);
            return removed;
        }

        public Task<bool> TryConsumeTimeStepAsync(Guid userId, long timeStep, CancellationToken cancellationToken = default)
        {
            // Bir kod, kendi dilimi + ileri tolerans kadar kabul edilebilir => işaret en az o kadar yaşamalı (+1 dilim pay).
            var ttl = TimeSpan.FromSeconds(TimeStepSeconds * (2 * _options.AllowedTimeStepDrift + 2));
            return _cacheService.SetIfNotExistsAsync($"{UsedTimeStepKeyPrefix}{userId}:{timeStep}", true, ttl, cancellationToken);
        }
    }
}
