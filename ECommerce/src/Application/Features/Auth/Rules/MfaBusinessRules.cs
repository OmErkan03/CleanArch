using System.Diagnostics.CodeAnalysis;
using ECommerce.Application.Features.Auth.Constants;
using Uludag.Core.Exceptions;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.Application.Features.Auth.Rules
{
    // MFA (ikinci faktör) adımlarının iş kuralları.
    public class MfaBusinessRules : BaseBusinessRules
    {
        private readonly IMfaChallengeStore _challengeStore;
        private readonly ITotpHelper _totpHelper;
        private readonly MfaOptions _options;

        public MfaBusinessRules(IMfaChallengeStore challengeStore, ITotpHelper totpHelper, MfaOptions options)
        {
            _challengeStore = challengeStore;
            _totpHelper = totpHelper;
            _options = options;
        }

        // MFA token'ı geçerli ve bu adım için verilmiş olmalı (kurulum token'ı ile doğrulama yapılamaz, tersi de).
        // 401: token, kimliğin yarısını (şifreyi) kanıtlıyor; geçersizse kullanıcı baştan giriş yapmalı.
        public Task ChallengeShouldBeValid([NotNull] MfaChallenge? challenge, MfaChallengePurpose expectedPurpose)
        {
            if (challenge is null || challenge.Purpose != expectedPurpose)
                throw new UnauthorizedException(AuthConstants.MfaTokenInvalid);

            return Task.CompletedTask;
        }

        // Onaylamadan önce /mfa/setup çağrılıp gizli anahtar üretilmiş olmalı.
        public Task SetupShouldBeStarted(MfaChallenge challenge)
        {
            if (string.IsNullOrEmpty(challenge.PendingEncryptedSecret))
                throw new BusinessException(AuthConstants.MfaSetupNotStarted);

            return Task.CompletedTask;
        }

        // Kullanıcı bu arada silinmişse ya da MFA durumu challenge açıldıktan sonra değiştiyse token geçersizdir.
        public Task UserShouldMatchChallenge([NotNull] Domain.Entities.User? user, MfaChallenge challenge)
        {
            var isValid = user is not null && challenge.Purpose switch
            {
                MfaChallengePurpose.Setup => !user.IsMfaEnabled,
                MfaChallengePurpose.Verify => user.IsMfaEnabled && !string.IsNullOrEmpty(user.MfaSecret),
                _ => false
            };

            if (!isValid)
                throw new UnauthorizedException(AuthConstants.MfaTokenInvalid);

            return Task.CompletedTask;
        }

        // Authenticator kodu doğru olmalı ve daha önce kullanılmamış olmalı.
        // Her yanlış denemede sayaç artar; sınır aşılırsa MFA token'ı iptal edilir (brute force koruması).
        public async Task CodeShouldBeValid(string mfaToken, MfaChallenge challenge, string encryptedSecret, string code)
        {
            var secret = _totpHelper.DecryptSecret(encryptedSecret);

            var isValid = _totpHelper.VerifyCode(secret, code, out var timeStep)
                // Kod doğru olsa bile aynı 30 sn'lik dilimde ikinci kez kullanılamaz (replay attack).
                && await _challengeStore.TryConsumeTimeStepAsync(challenge.UserId, timeStep);

            if (isValid)
                return;

            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= _options.MaxFailedAttempts)
            {
                await _challengeStore.RemoveAsync(mfaToken);
                throw new UnauthorizedException(AuthConstants.MfaTooManyAttempts);
            }

            await _challengeStore.UpdateAsync(mfaToken, challenge);
            throw new BusinessException(AuthConstants.MfaCodeInvalid);
        }
    }
}
