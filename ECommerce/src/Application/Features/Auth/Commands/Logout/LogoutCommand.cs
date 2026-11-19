using ECommerce.Application.Features.Auth.Services;
using Uludag.Core.Mediation;
using Uludag.Core.Security;

namespace ECommerce.Application.Features.Auth.Commands.Logout
{
    // Yalnızca bu isteği yapan oturumu (bu cihazı) kapatır: access oturumu VE ona bağlı refresh token ailesi.
    // JWT'nin kendisi iptal edilemez (imzası süresi dolana kadar geçerlidir); ama işaret ettiği oturum Redis'ten
    // silindiği için SessionTokenValidator token'ı reddeder => aynı token bir sonraki istekte 401 alır.
    // Refresh token da iptal edildiği için bu cihaz yeni access token alamaz.
    // IAllowAnonymousRequest YOK: oturumu olmayan biri çıkış yapamaz.
    public class LogoutCommand : IRequest
    {
        public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
        {
            private readonly ICurrentUser _currentUser;
            private readonly AuthSessionService _authSessionService;

            public LogoutCommandHandler(ICurrentUser currentUser, AuthSessionService authSessionService)
            {
                _currentUser = currentUser;
                _authSessionService = authSessionService;
            }

            public Task Handle(LogoutCommand request, CancellationToken cancellationToken) =>
                // AuthenticationBehavior oturum açılmış olmasını garanti eder; token'da her zaman "sid" vardır.
                _authSessionService.SignOutAsync(_currentUser.SessionKey!, cancellationToken);
        }
    }
}
