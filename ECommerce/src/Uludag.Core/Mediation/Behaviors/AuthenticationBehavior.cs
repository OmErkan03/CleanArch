using Uludag.Core.Exceptions;
using Uludag.Core.Security;

namespace Uludag.Core.Mediation.Behaviors
{
    // Handler çalışmadan ÖNCE araya girer ve kullanıcının oturum açmış olmasını şart koşar.
    //
    // "Varsayılan olarak güvenli" (secure by default): TÜM istekler korunur.
    // Herkese açık olması gereken istekler (Login, Register...) IAllowAnonymousRequest ile açıkça işaretlenir.
    // Böylece yeni bir Command/Query eklenip işaretlemek unutulursa sonuç "herkese açık bir açık" değil, "401" olur.
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
            // Anonim işaretli istekler kimlik doğrulama aşamasını atlar.
            if (request is IAllowAnonymousRequest)
                return next(cancellationToken);

            if (!_currentUser.IsAuthenticated)
                throw new UnauthorizedException();

            return next(cancellationToken);
        }
    }
}
