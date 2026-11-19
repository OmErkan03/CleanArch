using Uludag.Core.Mediation;
using Uludag.Core.Security;
using Uludag.Core.Security.Sessions;

namespace ECommerce.Application.Features.Auth.Commands.LogoutAll
{
    // Kullanıcının TÜM cihazlardaki oturumlarını (bu istek dahil) kapatır.
    // Ör. telefonu çalınan ya da hesabının başkasının elinde olduğundan şüphelenen kullanıcı için.
    public class LogoutAllCommand : IRequest<int>
    {
        public class LogoutAllCommandHandler : IRequestHandler<LogoutAllCommand, int>
        {
            private readonly ICurrentUser _currentUser;
            private readonly ISessionStore _sessionStore;

            public LogoutAllCommandHandler(ICurrentUser currentUser, ISessionStore sessionStore)
            {
                _currentUser = currentUser;
                _sessionStore = sessionStore;
            }

            // Kapatılan oturum sayısını döndürür.
            public Task<int> Handle(LogoutAllCommand request, CancellationToken cancellationToken) =>
                _sessionStore.RemoveUserSessionsAsync(_currentUser.UserId!.Value, cancellationToken);
        }
    }
}
