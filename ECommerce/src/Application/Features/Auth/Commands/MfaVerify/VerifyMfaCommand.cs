using ECommerce.Application.Features.Auth.Dtos;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Features.Auth.Services;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.Application.Features.Auth.Commands.MfaVerify
{
    // MFA'sı kurulu kullanıcının girişinin 2. adımı: Login "MfaVerificationRequired" döndüğünde,
    // MfaToken + authenticator uygulamasındaki kod ile çağrılır. Kod doğruysa JWT döner.
    public class VerifyMfaCommand : IRequest<LoginResponse>, IAllowAnonymousRequest
    {
        public string MfaToken { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, LoginResponse>
        {
            private readonly IMfaChallengeStore _challengeStore;
            private readonly IUserRepository _userRepository;
            private readonly MfaBusinessRules _mfaBusinessRules;
            private readonly AccessTokenService _accessTokenService;

            public VerifyMfaCommandHandler(
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

            public async Task<LoginResponse> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
            {
                var challenge = await _challengeStore.GetAsync(request.MfaToken, cancellationToken);
                await _mfaBusinessRules.ChallengeShouldBeValid(challenge, MfaChallengePurpose.Verify);

                var user = await _userRepository.GetByIdAsync(challenge.UserId, cancellationToken);
                await _mfaBusinessRules.UserShouldMatchChallenge(user, challenge);

                await _mfaBusinessRules.CodeShouldBeValid(request.MfaToken, challenge, user.MfaSecret!, request.Code);

                // MFA token tek kullanımlıktır.
                await _challengeStore.RemoveAsync(request.MfaToken, cancellationToken);

                return LoginResponse.Succeeded(await _accessTokenService.CreateAsync(user.Id, user.Email, cancellationToken));
            }
        }
    }
}
