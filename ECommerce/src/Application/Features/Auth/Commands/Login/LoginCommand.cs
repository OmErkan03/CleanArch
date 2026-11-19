using ECommerce.Application.Features.Auth.Constants;
using ECommerce.Application.Features.Auth.Dtos;
using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.MultiFactor;

namespace ECommerce.Application.Features.Auth.Commands.Login
{
    // Girişin İLK adımı: e-posta + şifre. E-posta/şifre hatalıysa AuthBusinessRules BusinessException fırlatır (400).
    //
    // Şifre doğru olsa bile JWT HEMEN verilmez; ikinci faktör (authenticator kodu) zorunludur:
    //   - MFA kurulmamışsa (ör. ilk giriş)  => Status = MfaSetupRequired       + MfaToken
    //   - MFA kuruluysa                    => Status = MfaVerificationRequired + MfaToken
    // JWT, MFA adımı tamamlanınca (ConfirmMfaSetupCommand / VerifyMfaCommand) verilir.
    //
    // IAllowAnonymousRequest => token almak için çağrılır, dolayısıyla token istenemez.
    public class LoginCommand : IRequest<LoginResponse>, IAllowAnonymousRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
        {
            private readonly IUserRepository _userRepository;
            private readonly AuthBusinessRules _authBusinessRules;
            private readonly IMfaChallengeStore _mfaChallengeStore;
            private readonly MfaOptions _mfaOptions;

            public LoginCommandHandler(
                IUserRepository userRepository,
                AuthBusinessRules authBusinessRules,
                IMfaChallengeStore mfaChallengeStore,
                MfaOptions mfaOptions)
            {
                _userRepository = userRepository;
                _authBusinessRules = authBusinessRules;
                _mfaChallengeStore = mfaChallengeStore;
                _mfaOptions = mfaOptions;
            }

            public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
                await _authBusinessRules.UserShouldExistWhenLoggingIn(user, request.Password);

                await _authBusinessRules.PasswordShouldMatch(user, request.Password);

                // Birinci faktör (bildiği şey: şifre) doğrulandı. İkinci faktör (sahip olduğu şey: telefon) için
                // Redis'te kısa ömürlü bir "yarım giriş" (challenge) açılır.
                var purpose = user.IsMfaEnabled ? MfaChallengePurpose.Verify : MfaChallengePurpose.Setup;
                var expiresAt = DateTime.UtcNow.AddMinutes(_mfaOptions.ChallengeExpirationMinutes);

                var mfaToken = await _mfaChallengeStore.CreateAsync(new MfaChallenge
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Purpose = purpose,
                    ExpiresAt = expiresAt
                }, cancellationToken);

                return purpose == MfaChallengePurpose.Setup
                    ? new LoginResponse
                    {
                        Status = LoginStatus.MfaSetupRequired,
                        MfaToken = mfaToken,
                        MfaTokenExpiration = expiresAt,
                        Message = AuthConstants.MfaSetupRequired
                    }
                    : new LoginResponse
                    {
                        Status = LoginStatus.MfaVerificationRequired,
                        MfaToken = mfaToken,
                        MfaTokenExpiration = expiresAt,
                        Message = AuthConstants.MfaVerificationRequired
                    };
            }
        }
    }
}
