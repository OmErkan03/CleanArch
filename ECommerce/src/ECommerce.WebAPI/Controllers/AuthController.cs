using ECommerce.Application.Features.Auth.Commands.Login;
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand loginCommand, CancellationToken cancellationToken)
        {
            // Hatalı e-posta/şifrede handler BusinessException fırlatır => global exception handler 400 döner.
            var accessToken = await sender.Send(loginCommand, cancellationToken);

            return Ok(accessToken);
        }

        // [Authorize] YOK: koruma GetMeQuery'nin ISecuredRequest olmasından gelir (AuthenticationBehavior).
        // Token yoksa / geçersizse => UnauthorizedException => 401.
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var me = await sender.Send(new GetMeQuery(), cancellationToken);

            return Ok(me);
        }
    }
}
