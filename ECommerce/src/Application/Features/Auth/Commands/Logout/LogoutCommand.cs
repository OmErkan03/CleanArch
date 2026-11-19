using ECommerce.Application.Features.Auth.Constants;
using ECommerce.Application.Features.Auth.Services;
using Uludag.Core.Exceptions;
using Uludag.Core.Mediation;
using Uludag.Core.Security;
using Uludag.Core.Security.RefreshTokens;

namespace ECommerce.Application.Features.Auth.Commands.Logout
{
    // Kullanıcıyı TÜM cihazlardan çıkarır: tüm access oturumları, tüm refresh token'lar ve bekleyen MFA token'ları
    // Redis'ten silinir (bkz. AuthSessionService.SignOutEverywhereAsync). Elde kalan hiçbir token artık işe yaramaz.
    //
    // Kullanıcı nasıl tanınır? İkisinden biri yeterlidir:
    //   1) Geçerli access token (JWT)  => oturumdaki kullanıcı
    //   2) Geçerli refresh token        => access token'ın süresi dolmuş olsa bile çıkış yapılabilsin diye
    // Bu yüzden IAllowAnonymousRequest: AuthenticationBehavior süresi dolmuş JWT'de isteği reddetmesin.
    // İkisi de yoksa / geçersizse 401.
    public class LogoutCommand : IRequest<SignOutResult>, IAllowAnonymousRequest
    {
        // WebAPI bunu HttpOnly cookie'den doldurur.
        public string? RefreshToken { get; set; }

        public class LogoutCommandHandler : IRequestHandler<LogoutCommand, SignOutResult>
        {
            private readonly ICurrentUser _currentUser;
            private readonly IRefreshTokenStore _refreshTokenStore;
            private readonly AuthSessionService _authSessionService;

            public LogoutCommandHandler(ICurrentUser currentUser, IRefreshTokenStore refreshTokenStore, AuthSessionService authSessionService)
            {
                _currentUser = currentUser;
                _refreshTokenStore = refreshTokenStore;
                _authSessionService = authSessionService;
            }

            public async Task<SignOutResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
            {
                var userId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;

                if (userId is null && !string.IsNullOrEmpty(request.RefreshToken))
                    userId = await _refreshTokenStore.GetUserIdAsync(request.RefreshToken, cancellationToken);

                if (userId is null)
                    throw new UnauthorizedException(AuthConstants.LogoutIdentityRequired);

                return await _authSessionService.SignOutEverywhereAsync(userId.Value, cancellationToken);
            }
        }
    }
}
