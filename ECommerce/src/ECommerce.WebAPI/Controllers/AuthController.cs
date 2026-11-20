using ECommerce.Application.Features.Auth.Commands.Login;
using ECommerce.Application.Features.Auth.Commands.Logout;
using ECommerce.Application.Features.Auth.Commands.MfaSetup;
using ECommerce.Application.Features.Auth.Commands.MfaSetupConfirm;
using ECommerce.Application.Features.Auth.Commands.MfaVerify;
using ECommerce.Application.Features.Auth.Commands.RefreshToken;
using ECommerce.Application.Features.Auth.Commands.Register;
using ECommerce.Application.Features.Auth.Dtos;
using ECommerce.Application.Features.Auth.Queries.GetMe;
using ECommerce.WebAPI.Security;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Exceptions;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(ISender sender) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand registerCommand, CancellationToken cancellationToken)
        {
            var id = await sender.Send(registerCommand, cancellationToken);

            return Ok(id);
        }

        // Girişin 1. adımı. Hatalı e-posta/şifrede handler BusinessException fırlatır => global exception handler 400 döner.
        // Şifre doğruysa JWT yerine MFA durumu döner:
        //   { "status": "MfaSetupRequired",        "mfaToken": "..." } => /mfa/setup + /mfa/setup/confirm
        //   { "status": "MfaVerificationRequired", "mfaToken": "..." } => /mfa/verify
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand loginCommand, CancellationToken cancellationToken)
        {
            var response = await sender.Send(loginCommand, cancellationToken);

            return Ok(response);
        }

        // MFA kurulumu 1/2: gizli anahtarı ve QR'a çevrilecek otpauth:// adresini döndürür.
        [HttpPost("mfa/setup")]
        public async Task<IActionResult> SetupMfa([FromBody] SetupMfaCommand command, CancellationToken cancellationToken)
        {
            var response = await sender.Send(command, cancellationToken);

            return Ok(response);
        }

        // MFA kurulumu 2/2: authenticator'daki ilk kod ile kurulumu onaylar; MFA açılır.
        // Gövdede access token (JWT), HttpOnly cookie'de refresh token döner.
        [HttpPost("mfa/setup/confirm")]
        public async Task<IActionResult> ConfirmMfaSetup([FromBody] ConfirmMfaSetupCommand command, CancellationToken cancellationToken)
        {
            var response = await sender.Send(command, cancellationToken);

            return SignedIn(response);
        }

        // MFA kurulu kullanıcının girişinin 2. adımı: kod doğruysa gövdede JWT, cookie'de refresh token döner.
        [HttpPost("mfa/verify")]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaCommand command, CancellationToken cancellationToken)
        {
            var response = await sender.Send(command, cancellationToken);

            return SignedIn(response);
        }

        // Refresh token (cookie) ile yeni access token alır; şifre/MFA gerekmez. Gövde boştur.
        // Refresh token tek kullanımlıktır: yanıtta cookie yenisiyle değiştirilir.
        // Geçersiz / süresi dolmuş / daha önce kullanılmış token => 401 ve cookie silinir (yeniden giriş gerekir).
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            try
            {
                var response = await sender.Send(
                    new RefreshTokenCommand { RefreshToken = RefreshTokenCookie.Read(Request) ?? string.Empty }, cancellationToken);

                return SignedIn(response);
            }
            catch (UnauthorizedException)
            {
                // İşe yaramayan cookie istemcide kalmasın; hata yine global exception handler'a gider (401).
                RefreshTokenCookie.Delete(Response);
                throw;
            }
        }

        // TÜM cihazlardan çıkış: kullanıcının tüm access oturumları, refresh token'ları ve bekleyen MFA token'ları
        // Redis'ten silinir; iptal edilenlerin sayısı döner.
        // Kullanıcı geçerli access token (Authorization başlığı) YA DA refresh token cookie'si ile tanınır;
        // böylece access token'ı süresi dolmuş kullanıcı da çıkış yapabilir.
        // Cookie her durumda (başarısız olsa bile) istemciden silinir.
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            RefreshTokenCookie.Delete(Response);

            var result = await sender.Send(new LogoutCommand { RefreshToken = RefreshTokenCookie.Read(Request) }, cancellationToken);

            return Ok(result);
        }

        // Giriş tamamlandıysa refresh token cookie'ye yazılır (gövdeye JsonIgnore ile hiç girmez).
        private OkObjectResult SignedIn(LoginResponse response)
        {
            if (response.RefreshToken is not null && response.RefreshTokenExpiration is not null)
                RefreshTokenCookie.Append(Response, response.RefreshToken, response.RefreshTokenExpiration.Value);

            return Ok(response);
        }

        // [Authorize] YOK: koruma AuthenticationBehavior'dan gelir (GetMeQuery IAllowAnonymousRequest değil).
        // Token yoksa / geçersizse => UnauthorizedException => 401.
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var me = await sender.Send(new GetMeQuery(), cancellationToken);

            return Ok(me);
        }
    }
}
