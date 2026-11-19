using Uludag.Core.Mediation;
using Uludag.Core.Security;
using Uludag.Core.Security.Sessions;

namespace ECommerce.Application.Features.Auth.Commands.Logout
{
    // Yalnızca bu isteği yapan oturumu (bu cihazı) kapatır.
    // JWT'nin kendisi iptal edilemez (imzası süresi dolana kadar geçerlidir); ama işaret ettiği oturum Redis'ten
    // silindiği için SessionTokenValidator token'ı reddeder => aynı token bir sonraki istekte 401 alır.
    // IAllowAnonymousRequest YOK: oturumu olmayan biri çıkış yapamaz.
    public class LogoutCommand : IRequest
    {
        public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
        {
            private readonly ICurrentUser _currentUser;
            private readonly ISessionStore _sessionStore;

            public LogoutCommandHandler(ICurrentUser currentUser, ISessionStore sessionStore)
            {
                _currentUser = currentUser;
                _sessionStore = sessionStore;
            }

            public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
            {
                // AuthenticationBehavior oturum açılmış olmasını garanti eder; token'da her zaman "sid" vardır.
                await _sessionStore.RemoveAsync(_currentUser.SessionKey!, cancellationToken);
            }
        }
    }
}
