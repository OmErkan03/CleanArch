using Uludag.Core.Exceptions;
using Uludag.Core.Security;
using Uludag.Core.Security.Constants;

namespace Uludag.Core.Mediation.Behaviors
{
    // AuthenticationBehavior "kim olduğunu biliyor muyuz?" (401) sorusunu sorar,
    // bu behavior ise "bu işlemi yapmaya YETKİSİ var mı?" (403) sorusunu sorar.
    //
    // Yetki kontrolü metinseldir: isteğin istediği claim adları (ISecuredRequest.Roles) ile
    // kullanıcının token'ındaki operation claim adları birebir (büyük/küçük harf duyarlı) karşılaştırılır.
    // Kullanıcının claim'leri Login sırasında veritabanından (User <-> OperationClaim) okunup token'a yazılır;
    // bu yüzden her istekte veritabanına gidilmez. Claim değişikliği kullanıcının tekrar giriş yapmasıyla geçerli olur.
    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull, IRequest<TResponse>
    {
        private readonly ICurrentUser _currentUser;

        public AuthorizationBehavior(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Yetki tanımlamayan istekler için yalnızca kimlik doğrulama (AuthenticationBehavior) yeterlidir.
            if (request is not ISecuredRequest securedRequest)
                return next(cancellationToken);

            // Yetki isteyen bir istek anonim çalıştırılamaz (IAllowAnonymousRequest ile birlikte işaretlense bile).
            if (!_currentUser.IsAuthenticated)
                throw new UnauthorizedException();

            var userClaims = _currentUser.OperationClaims;

            // Genel "Admin" claim'i tüm işlemlere izin verir.
            var isAuthorized = userClaims.Contains(GeneralOperationClaims.Admin, StringComparer.Ordinal)
                || securedRequest.Roles.Any(role => userClaims.Contains(role, StringComparer.Ordinal));

            if (!isAuthorized)
                throw new ForbiddenException();

            return next(cancellationToken);
        }
    }
}
