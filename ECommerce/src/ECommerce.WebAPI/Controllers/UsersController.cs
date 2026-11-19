using ECommerce.Application.Features.User.Commands.ResetMfa;
using ECommerce.Application.Features.UserOperationClaim.Commands.Assign;
using ECommerce.Application.Features.UserOperationClaim.Commands.Revoke;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(ISender sender) : ControllerBase
    {
        // Kullanıcıya yetki verir. Yalnızca Admin (bkz. AssignOperationClaimCommand.Roles).
        [HttpPost("{userId:guid}/operation-claims/{operationClaimId:int}")]
        public async Task<IActionResult> AssignOperationClaim([FromRoute] Guid userId, [FromRoute] int operationClaimId, CancellationToken cancellationToken)
        {
            await sender.Send(new AssignOperationClaimCommand { UserId = userId, OperationClaimId = operationClaimId }, cancellationToken);

            return NoContent();
        }

        // Kullanıcıdan yetkiyi geri alır. Yalnızca Admin.
        [HttpDelete("{userId:guid}/operation-claims/{operationClaimId:int}")]
        public async Task<IActionResult> RevokeOperationClaim([FromRoute] Guid userId, [FromRoute] int operationClaimId, CancellationToken cancellationToken)
        {
            await sender.Send(new RevokeOperationClaimCommand { UserId = userId, OperationClaimId = operationClaimId }, cancellationToken);

            return NoContent();
        }

        // Kullanıcının MFA'sını sıfırlar ve tüm oturumlarını kapatır (telefon kaybı vb.). Yalnızca Admin.
        // Kullanıcı bir sonraki girişinde authenticator'ı yeniden kurar.
        [HttpDelete("{userId:guid}/mfa")]
        public async Task<IActionResult> ResetMfa([FromRoute] Guid userId, CancellationToken cancellationToken)
        {
            await sender.Send(new ResetUserMfaCommand { UserId = userId }, cancellationToken);

            return NoContent();
        }
    }
}
