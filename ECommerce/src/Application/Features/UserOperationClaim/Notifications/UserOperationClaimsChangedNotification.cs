using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.Sessions;

namespace ECommerce.Application.Features.UserOperationClaim.Notifications
{
    // "Bu kullanıcının yetkileri değişti" olayı.
    // Kullanıcının yetkilerini değiştiren HER işlem, veritabanına kaydettikten SONRA bu bildirimi yayınlar (IPublisher.Publish).
    // Böylece yetki değiştiren kod, Redis'teki oturumların nasıl güncellendiğini bilmek zorunda kalmaz;
    // ileride yetki değiştiren yeni bir yer eklenirse yalnızca bu bildirimi yayınlaması yeterlidir.
    public class UserOperationClaimsChangedNotification : INotification
    {
        public Guid UserId { get; init; }

        // Kullanıcının Redis'teki tüm açık oturumlarındaki yetkileri günceller.
        // Oturum her istekte Redis'ten okunduğu için (bkz. WebAPI => SessionTokenValidator) yeni yetkiler
        // kullanıcının BİR SONRAKİ isteğinde geçerli olur; yeniden giriş yapması ya da yeni token alması gerekmez.
        public class RefreshUserSessionsHandler : INotificationHandler<UserOperationClaimsChangedNotification>
        {
            private readonly IUserRepository _userRepository;
            private readonly ISessionStore _sessionStore;

            public RefreshUserSessionsHandler(IUserRepository userRepository, ISessionStore sessionStore)
            {
                _userRepository = userRepository;
                _sessionStore = sessionStore;
            }

            public async Task Handle(UserOperationClaimsChangedNotification notification, CancellationToken cancellationToken)
            {
                // Yetkiler bildirimden değil veritabanından okunur: aynı anda iki değişiklik yapılsa bile
                // Redis'e en son kaydedilmiş (doğru) liste yazılır.
                var operationClaims = await _userRepository.GetOperationClaimNamesAsync(notification.UserId, cancellationToken);

                await _sessionStore.UpdateUserSessionsAsync(
                    notification.UserId,
                    session => session.OperationClaims = operationClaims,
                    cancellationToken);
            }
        }
    }
}
