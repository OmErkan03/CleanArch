using System.Security.Cryptography;
using Uludag.Core.Caching;

namespace Uludag.Core.Security.MultiFactor
{
    // IMfaChallengeStore'un ICacheService (Redis) üzerinde çalışan implementasyonu.
    //
    // Redis'teki anahtarlar:
    //   mfa-challenge:{mfaToken}          => MfaChallenge JSON'ı. TTL = MfaOptions.ChallengeExpirationMinutes
    //   mfa-used:{userId}:{timeStep}      => kullanılmış kod işareti. TTL = kodun geçerli kalabileceği süre
    public class CacheMfaChallengeStore : IMfaChallengeStore
    {
        private const string ChallengeKeyPrefix = "mfa-challenge:";
        private const string UsedTimeStepKeyPrefix = "mfa-used:";
        private const int TimeStepSeconds = 30;

        private readonly ICacheService _cacheService;
        private readonly MfaOptions _options;

        public CacheMfaChallengeStore(ICacheService cacheService, MfaOptions options)
        {
            _cacheService = cacheService;
            _options = options;
        }

        private static string ChallengeCacheKey(string mfaToken) => ChallengeKeyPrefix + mfaToken;

        public async Task<string> CreateAsync(MfaChallenge challenge, CancellationToken cancellationToken = default)
        {
            var mfaToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

            var ttl = challenge.ExpiresAt - DateTime.UtcNow;
            if (ttl <= TimeSpan.Zero)
                throw new InvalidOperationException("MFA challenge'ın bitiş zamanı geçmişte olamaz.");

            await _cacheService.SetAsync(ChallengeCacheKey(mfaToken), challenge, ttl, cancellationToken);
            return mfaToken;
        }

        public Task<MfaChallenge?> GetAsync(string mfaToken, CancellationToken cancellationToken = default) =>
            string.IsNullOrWhiteSpace(mfaToken)
                ? Task.FromResult<MfaChallenge?>(null)
                : _cacheService.GetAsync<MfaChallenge>(ChallengeCacheKey(mfaToken), cancellationToken);

        public Task<bool> UpdateAsync(string mfaToken, MfaChallenge challenge, CancellationToken cancellationToken = default) =>
            _cacheService.ReplaceAsync(ChallengeCacheKey(mfaToken), challenge, cancellationToken);

        public Task RemoveAsync(string mfaToken, CancellationToken cancellationToken = default) =>
            _cacheService.RemoveAsync(ChallengeCacheKey(mfaToken), cancellationToken);

        public Task<bool> TryConsumeTimeStepAsync(Guid userId, long timeStep, CancellationToken cancellationToken = default)
        {
            // Bir kod, kendi dilimi + ileri tolerans kadar kabul edilebilir => işaret en az o kadar yaşamalı (+1 dilim pay).
            var ttl = TimeSpan.FromSeconds(TimeStepSeconds * (2 * _options.AllowedTimeStepDrift + 2));
            return _cacheService.SetIfNotExistsAsync($"{UsedTimeStepKeyPrefix}{userId}:{timeStep}", true, ttl, cancellationToken);
        }
    }
}
