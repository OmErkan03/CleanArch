using ECommerce.Application.Features.Auth.Constants;
using ECommerce.Application.Features.Auth.Rules;
using Uludag.Core.Exceptions;
using Uludag.Core.Mediation;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.Application.Features.Auth.Commands.MfaSetup
{
    // MFA kurulumunun 1. adımı: gizli anahtar üretir ve QR koda çevrilecek adresi döndürür.
    // Login "MfaSetupRequired" döndüğünde, verdiği MfaToken ile çağrılır. Kullanıcının henüz JWT'si olmadığı için anonimdir;
    // kimliği MfaToken ile (şifresini doğrulamış olduğu) kanıtlanır.
    // Tekrar çağrılırsa yeni bir anahtar üretilir (ör. QR'ı okutamadıysa); öncekiler geçersiz olur.
    public class SetupMfaCommand : IRequest<SetupMfaResponse>, IAllowAnonymousRequest
    {
        public string MfaToken { get; set; } = string.Empty;

        public class SetupMfaCommandHandler : IRequestHandler<SetupMfaCommand, SetupMfaResponse>
        {
            private readonly IMfaChallengeStore _challengeStore;
            private readonly ITotpHelper _totpHelper;
            private readonly MfaBusinessRules _mfaBusinessRules;

            public SetupMfaCommandHandler(IMfaChallengeStore challengeStore, ITotpHelper totpHelper, MfaBusinessRules mfaBusinessRules)
            {
                _challengeStore = challengeStore;
                _totpHelper = totpHelper;
                _mfaBusinessRules = mfaBusinessRules;
            }

            public async Task<SetupMfaResponse> Handle(SetupMfaCommand request, CancellationToken cancellationToken)
            {
                var challenge = await _challengeStore.GetAsync(request.MfaToken, cancellationToken);
                await _mfaBusinessRules.ChallengeShouldBeValid(challenge, MfaChallengePurpose.Setup);

                var secret = _totpHelper.GenerateSecret();

                // Anahtar henüz kullanıcıya YAZILMAZ: kullanıcı ilk doğru kodu girip kurulumu onaylayana kadar Redis'te (şifreli) bekler.
                // Böylece QR'ı okutmadan yarıda bırakan kullanıcının hesabı kilitlenmez.
                challenge.PendingEncryptedSecret = _totpHelper.EncryptSecret(secret);
                // false => token bu arada süresi doldu / iptal edildi.
                if (!await _challengeStore.UpdateAsync(request.MfaToken, challenge, cancellationToken))
                    throw new UnauthorizedException(AuthConstants.MfaTokenInvalid);

                return new SetupMfaResponse
                {
                    // Gizli anahtar YALNIZCA burada, bir kez gösterilir. QR okutulamazsa uygulamaya elle girilebilir.
                    Secret = secret,
                    ProvisioningUri = _totpHelper.GenerateProvisioningUri(secret, challenge.Email)
                };
            }
        }
    }

    public class SetupMfaResponse
    {
        public string Secret { get; set; } = string.Empty;

        // otpauth://totp/... => istemci bunu QR koda çevirip gösterir (sunucu görsel üretmez).
        public string ProvisioningUri { get; set; } = string.Empty;
    }
}
