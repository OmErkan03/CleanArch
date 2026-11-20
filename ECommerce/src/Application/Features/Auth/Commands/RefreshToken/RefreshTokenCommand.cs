using ECommerce.Application.Features.Auth.Dtos;
using ECommerce.Application.Features.Auth.Services;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Auth.Commands.RefreshToken
{
    // Access token'ın süresi dolduğunda (ya da dolmadan önce) refresh token ile yenisini alır; şifre/MFA gerekmez.
    // Refresh token tek kullanımlıktır: yanıtla birlikte yenisi verilir, gönderilen artık geçersizdir.
    // IAllowAnonymousRequest: access token'ı süresi dolmuş kullanıcı çağırır; kimliği refresh token ile kanıtlanır.
    public class RefreshTokenCommand : IRequest<LoginResponse>, IAllowAnonymousRequest
    {
        // WebAPI bunu HttpOnly cookie'den doldurur (istemci gövdede göndermez).
        public string RefreshToken { get; set; } = string.Empty;

        public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
        {
            private readonly AuthSessionService _authSessionService;

            public RefreshTokenCommandHandler(AuthSessionService authSessionService)
            {
                _authSessionService = authSessionService;
            }

            public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
                LoginResponse.Succeeded(await _authSessionService.RefreshAsync(request.RefreshToken, cancellationToken));
        }
    }
}
