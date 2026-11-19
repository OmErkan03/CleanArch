using ECommerce.Application.Features.Auth.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.JWT;
using Uludag.Core.Security.Sessions;

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
            private readonly ISessionStore _sessionStore;
            private readonly TokenOptions _tokenOptions;

            public LoginCommandHandler(
                IUserRepository userRepository,
                AuthBusinessRules authBusinessRules,
                ITokenHelper tokenHelper,
                ISessionStore sessionStore,
                TokenOptions tokenOptions)
            {
                _userRepository = userRepository;
                _authBusinessRules = authBusinessRules;
                _tokenHelper = tokenHelper;
                _sessionStore = sessionStore;
                _tokenOptions = tokenOptions;
            }

            public async Task<AccessToken> Handle(LoginCommand request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
                await _authBusinessRules.UserShouldExistWhenLoggingIn(user, request.Password);

                await _authBusinessRules.PasswordShouldMatch(user, request.Password);

                // Kullanıcının yetkileri Redis'teki oturuma yazılır; AuthorizationBehavior her istekte DB'ye gitmeden bunları kontrol eder.
                var operationClaims = await _userRepository.GetOperationClaimNamesAsync(user.Id, cancellationToken);

                var now = DateTime.UtcNow;
                var expiration = now.AddMinutes(_tokenOptions.AccessTokenExpirationMinutes);

                // Kullanıcı bilgileri Redis'e yazılır (TTL = token ömrü), karşılığında rastgele bir oturum anahtarı alınır.
                var sessionKey = await _sessionStore.CreateAsync(new UserSession
                {
                    UserId = user.Id,
                    Email = user.Email,
                    OperationClaims = operationClaims.ToList(),
                    CreatedAt = now,
                    ExpiresAt = expiration
                }, cancellationToken);

                // Token yalnızca bu anahtarı taşır; kullanıcı bilgileri her istekte Redis'ten okunur.
                return _tokenHelper.CreateToken(sessionKey, expiration);
            }
        }
    }
}
