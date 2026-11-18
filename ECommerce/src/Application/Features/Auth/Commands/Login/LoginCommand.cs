using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;

namespace ECommerce.Application.Features.Auth.Commands.Login
{
    // IRequest<AccessToken> => başarılı girişte JWT döner.
    // E-posta/şifre hatalıysa AuthBusinessRules BusinessException fırlatır (400).
    // IAllowAnonymousRequest => token almak için çağrılır, dolayısıyla token istenemez.
    public class LoginCommand : IRequest<AccessToken>, IAllowAnonymousRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public class LoginCommandHandler : IRequestHandler<LoginCommand, AccessToken>
        {
            private readonly IUserRepository _userRepository;
            private readonly AuthBusinessRules _authBusinessRules;
            private readonly ITokenHelper _tokenHelper;

            public LoginCommandHandler(IUserRepository userRepository, AuthBusinessRules authBusinessRules, ITokenHelper tokenHelper)
            {
                _userRepository = userRepository;
                _authBusinessRules = authBusinessRules;
                _tokenHelper = tokenHelper;
            }

            public async Task<AccessToken> Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
                await _authBusinessRules.UserShouldExistWhenLoggingIn(user, request.Password);

                await _authBusinessRules.PasswordShouldMatch(user, request.Password);

                // Kullanıcının yetkileri token'a yazılır; AuthorizationBehavior her istekte DB'ye gitmeden bunları kontrol eder.
                var operationClaims = await _userRepository.GetOperationClaimNamesAsync(user.Id, cancellationToken);

                // Kimlik doğrulandı => kullanıcı bilgilerini taşıyan, imzalı bir token üretilir.
                return _tokenHelper.CreateToken(user.Id, user.Email, operationClaims);
            }
        }
    }
}
