using Uludag.Core.Exceptions;
using Uludag.Core.Security;

namespace Uludag.Core.Mediation.Behaviors
{
    // Handler çalışmadan ÖNCE araya girer: istek ISecuredRequest ise kullanıcının oturum açmış olmasını şart koşar.
    //
    // Token'ın kendisini (imza, süre, issuer, audience) burada doğrulamayız; o iş ASP.NET'in JwtBearer middleware'inindir
    // (Program.cs => AddJwtBearer + UseAuthentication). Middleware geçerli token gördüğünde HttpContext.User'ı doldurur,
    // ICurrentUser da oradan okur. Bu behavior yalnızca "kullanıcı doğrulanmış mı?" sorusunu sorar.
    //
    // Controller'da [Authorize] yerine bu yaklaşımın faydası: güvenlik kuralı isteğin (Command/Query) üzerinde durur,
    // istek hangi kanaldan (controller, background job, test) gönderilirse gönderilsin kontrol çalışır.
    public class AuthenticationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull, IRequest<TResponse>
    {
        private readonly ICurrentUser _currentUser;

        public AuthenticationBehavior(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Korumasız istekler (Login, Register, ürün listeleme...) olduğu gibi devam eder.
            if (request is ISecuredRequest && !_currentUser.IsAuthenticated)
                throw new UnauthorizedException();

            return next(cancellationToken);
        }
    }
}
