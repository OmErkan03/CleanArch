using Uludag.Core.Mediation;
using Uludag.Core.Security;

namespace ECommerce.Application.Features.Auth.Queries.GetMe
{
    // Ek bir işaret gerekmez: IAllowAnonymousRequest olmadığı için AuthenticationBehavior,
    // geçerli token yoksa handler'a hiç ulaşmadan 401 döndürür.
    // Bu sayede handler içinde "kullanıcı giriş yapmış mı?" kontrolü yazmaya gerek kalmaz.
    public class GetMeQuery : IRequest<GetMeResponse>
    {
        public class GetMeQueryHandler : IRequestHandler<GetMeQuery, GetMeResponse>
        {
            private readonly ICurrentUser _currentUser;

            public GetMeQueryHandler(ICurrentUser currentUser)
            {
                _currentUser = currentUser;
            }

            public Task<GetMeResponse> Handle(GetMeQuery request, CancellationToken cancellationToken)
            {
                // Bilgiler veritabanından değil, token'daki anahtarla Redis'ten okunan oturumdan gelir
                // (SessionTokenValidator bunları HttpContext.User'a claim olarak ekler).
                return Task.FromResult(new GetMeResponse
                {
                    Id = _currentUser.UserId!.Value,
                    Email = _currentUser.Email!,
                    OperationClaims = _currentUser.OperationClaims.ToList()
                });
            }
        }
    }

    public class GetMeResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<string> OperationClaims { get; set; } = [];
    }
}
