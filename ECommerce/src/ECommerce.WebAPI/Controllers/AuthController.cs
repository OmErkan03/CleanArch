using ECommerce.Application.Features.Auth.Commands.Login;
using ECommerce.Application.Features.Auth.Commands.MfaSetup;
using ECommerce.Application.Features.Auth.Commands.MfaSetupConfirm;
using ECommerce.Application.Features.Auth.Commands.MfaVerify;
using ECommerce.Application.Features.Auth.Commands.Register;
using ECommerce.Application.Features.Auth.Queries.GetMe;
using Microsoft.AspNetCore.Mvc;
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

        // MFA kurulumu 2/2: authenticator'daki ilk kod ile kurulumu onaylar; MFA açılır ve JWT döner.
        [HttpPost("mfa/setup/confirm")]
        public async Task<IActionResult> ConfirmMfaSetup([FromBody] ConfirmMfaSetupCommand command, CancellationToken cancellationToken)
        {
            var response = await sender.Send(command, cancellationToken);

            return Ok(response);
        }

        // MFA kurulu kullanıcının girişinin 2. adımı: kod doğruysa JWT döner.
        [HttpPost("mfa/verify")]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaCommand command, CancellationToken cancellationToken)
        {
            var response = await sender.Send(command, cancellationToken);

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
