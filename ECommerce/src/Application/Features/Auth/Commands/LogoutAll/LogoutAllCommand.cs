using ECommerce.Application.Features.Auth.Services;
using Uludag.Core.Mediation;
using Uludag.Core.Security;

namespace ECommerce.Application.Features.Auth.Commands.LogoutAll
{
    // Kullanıcının TÜM cihazlardaki oturumlarını ve refresh token'larını (bu istek dahil) iptal eder.
    // Ör. telefonu çalınan ya da hesabının başkasının elinde olduğundan şüphelenen kullanıcı için.
    public class LogoutAllCommand : IRequest<int>
    {
        public class LogoutAllCommandHandler : IRequestHandler<LogoutAllCommand, int>
        {
            private readonly ICurrentUser _currentUser;
            private readonly AuthSessionService _authSessionService;

            public LogoutAllCommandHandler(ICurrentUser currentUser, AuthSessionService authSessionService)
            {
                _currentUser = currentUser;
                _authSessionService = authSessionService;
            }

            // Kapatılan oturum sayısını döndürür.
            public Task<int> Handle(LogoutAllCommand request, CancellationToken cancellationToken) =>
                _authSessionService.SignOutEverywhereAsync(_currentUser.UserId!.Value, cancellationToken);
        }
    }
}
