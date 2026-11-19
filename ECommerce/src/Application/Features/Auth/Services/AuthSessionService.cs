using ECommerce.Application.Services.Repositories;
using Uludag.Core.Security.JWT;
using Uludag.Core.Security.Sessions;

namespace ECommerce.Application.Features.Auth.Services
{
    // Kimliği TAMAMEN doğrulanmış (şifre + MFA) kullanıcı için oturum açar ve JWT üretir.
    // MFA doğrulama ve MFA kurulum onayı aynı adımı yaptığı için tek yerde toplanmıştır.
    public class AccessTokenService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionStore _sessionStore;
        private readonly ITokenHelper _tokenHelper;
        private readonly TokenOptions _tokenOptions;

        public AccessTokenService(IUserRepository userRepository, ISessionStore sessionStore, ITokenHelper tokenHelper, TokenOptions tokenOptions)
        {
            _userRepository = userRepository;
            _sessionStore = sessionStore;
            _tokenHelper = tokenHelper;
            _tokenOptions = tokenOptions;
        }

        public async Task<AccessToken> CreateAsync(Guid userId, string email, CancellationToken cancellationToken = default)
        {
            // Kullanıcının yetkileri Redis'teki oturuma yazılır; AuthorizationBehavior her istekte DB'ye gitmeden bunları kontrol eder.
            var operationClaims = await _userRepository.GetOperationClaimNamesAsync(userId, cancellationToken);

            var now = DateTime.UtcNow;
            var expiration = now.AddMinutes(_tokenOptions.AccessTokenExpirationMinutes);

            // Kullanıcı bilgileri Redis'e yazılır (TTL = token ömrü), karşılığında rastgele bir oturum anahtarı alınır.
            var sessionKey = await _sessionStore.CreateAsync(new UserSession
            {
                UserId = userId,
                Email = email,
                OperationClaims = operationClaims,
                CreatedAt = now,
                ExpiresAt = expiration
            }, cancellationToken);

            // Token yalnızca bu anahtarı taşır; kullanıcı bilgileri her istekte Redis'ten okunur.
            return _tokenHelper.CreateToken(sessionKey, expiration);
        }
    }
}
