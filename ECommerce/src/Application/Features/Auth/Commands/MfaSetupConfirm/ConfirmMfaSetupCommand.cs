using ECommerce.Application.Features.Auth.Dtos;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Features.Auth.Services;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.Application.Features.Auth.Commands.MfaSetupConfirm
{
    // MFA kurulumunun 2. adımı: kullanıcı QR'ı okuttuktan sonra uygulamadaki kodu girer.
    // Kod doğruysa telefon ile sunucunun AYNI anahtara sahip olduğu kanıtlanmış olur => anahtar kullanıcıya kaydedilir,
    // MFA açılır ve giriş tamamlanır (JWT döner).
    public class ConfirmMfaSetupCommand : IRequest<LoginResponse>, IAllowAnonymousRequest
    {
        public string MfaToken { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public class ConfirmMfaSetupCommandHandler : IRequestHandler<ConfirmMfaSetupCommand, LoginResponse>
        {
            private readonly IMfaChallengeStore _challengeStore;
            private readonly IUserRepository _userRepository;
            private readonly MfaBusinessRules _mfaBusinessRules;
            private readonly AccessTokenService _accessTokenService;

            public ConfirmMfaSetupCommandHandler(
                IMfaChallengeStore challengeStore,
                IUserRepository userRepository,
                MfaBusinessRules mfaBusinessRules,
                AccessTokenService accessTokenService)
            {
                _challengeStore = challengeStore;
                _userRepository = userRepository;
                _mfaBusinessRules = mfaBusinessRules;
                _accessTokenService = accessTokenService;
            }

            public async Task<LoginResponse> Handle(ConfirmMfaSetupCommand request, CancellationToken cancellationToken)
            {
                var challenge = await _challengeStore.GetAsync(request.MfaToken, cancellationToken);
                await _mfaBusinessRules.ChallengeShouldBeValid(challenge, MfaChallengePurpose.Setup);
                await _mfaBusinessRules.SetupShouldBeStarted(challenge);

                var user = await _userRepository.GetByIdAsync(challenge.UserId, cancellationToken);
                await _mfaBusinessRules.UserShouldMatchChallenge(user, challenge);

                await _mfaBusinessRules.CodeShouldBeValid(request.MfaToken, challenge, challenge.PendingEncryptedSecret!, request.Code);

                // Anahtar zaten şifreli; olduğu gibi kullanıcıya yazılır.
                user.MfaSecret = challenge.PendingEncryptedSecret;
                user.IsMfaEnabled = true;
                await _userRepository.SaveChangesAsync(cancellationToken);

                // MFA token tek kullanımlıktır.
                await _challengeStore.RemoveAsync(request.MfaToken, cancellationToken);

                return LoginResponse.Succeeded(await _accessTokenService.CreateAsync(user.Id, user.Email, cancellationToken));
            }
        }
    }
}
