using ECommerce.Application.Features.User.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.Constants;
using Uludag.Core.Security.Sessions;

namespace ECommerce.Application.Features.User.Commands.ResetMfa
{
    // Kullanıcının MFA'sını sıfırlar (ör. telefonunu kaybetti, authenticator uygulamasını sildi).
    // Sonuç: gizli anahtar silinir, MFA kapanır ve kullanıcının TÜM oturumları sonlandırılır.
    // Kullanıcı bir sonraki girişinde "MfaSetupRequired" alır ve yeni cihazıyla kurulumu baştan yapar.
    //
    // Yalnızca Admin: MFA'yı kullanıcının kendisinin kapatabilmesi, ikinci faktörün amacını boşa çıkarırdı
    // (token'ı ele geçiren biri MFA'yı kapatıp kendi telefonunu bağlayabilirdi). Admin, kimliği başka bir kanaldan doğrular.
    public class ResetUserMfaCommand : IRequest, ISecuredRequest
    {
        public string[] Roles => [GeneralOperationClaims.Admin];

        public Guid UserId { get; set; }

        public class ResetUserMfaCommandHandler : IRequestHandler<ResetUserMfaCommand>
        {
            private readonly IUserRepository _userRepository;
            private readonly ISessionStore _sessionStore;
            private readonly UserBusinessRules _userBusinessRules;

            public ResetUserMfaCommandHandler(IUserRepository userRepository, ISessionStore sessionStore, UserBusinessRules userBusinessRules)
            {
                _userRepository = userRepository;
                _sessionStore = sessionStore;
                _userBusinessRules = userBusinessRules;
            }

            public async Task Handle(ResetUserMfaCommand request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                await _userBusinessRules.UserShouldExist(user);
                await _userBusinessRules.MfaShouldBeEnabled(user);

                user.IsMfaEnabled = false;
                user.MfaSecret = null;
                await _userRepository.SaveChangesAsync(cancellationToken);

                // Eski cihazla açılmış oturumlar da kapanır (kayıp telefon başkasının elinde olabilir).
                // Yarım kalmış "Verify" MFA token'ları ayrıca silinmez: MfaBusinessRules.UserShouldMatchChallenge
                // MFA'sı kapalı kullanıcı için doğrulama token'ını zaten reddeder.
                await _sessionStore.RemoveUserSessionsAsync(user.Id, cancellationToken);
            }
        }
    }
}
