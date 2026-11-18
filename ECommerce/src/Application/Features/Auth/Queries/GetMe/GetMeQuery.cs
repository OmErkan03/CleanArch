using Uludag.Core.Mediation;
using Uludag.Core.Security;

namespace ECommerce.Application.Features.Auth.Queries.GetMe
{
    // ISecuredRequest => AuthenticationBehavior, geçerli token yoksa handler'a hiç ulaşmadan 401 döndürür.
    // Bu sayede handler içinde "kullanıcı giriş yapmış mı?" kontrolü yazmaya gerek kalmaz.
    public class GetMeQuery : IRequest<GetMeResponse>, ISecuredRequest
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
                // Bilgiler veritabanından değil, token'ın içindeki claim'lerden okunur.
                return Task.FromResult(new GetMeResponse
                {
                    Id = _currentUser.UserId!.Value,
                    Email = _currentUser.Email!
                });
            }
        }
    }

    public class GetMeResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
