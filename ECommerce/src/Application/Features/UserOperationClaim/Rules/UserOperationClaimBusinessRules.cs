using System.Diagnostics.CodeAnalysis;
using ECommerce.Application.Features.UserOperationClaim.Constants;
using Uludag.Core.Exceptions;

namespace ECommerce.Application.Features.UserOperationClaim.Rules
{
    // Kullanıcıya yetki verme / yetkiyi geri alma ile ilgili iş kuralları.
    public class UserOperationClaimBusinessRules : BaseBusinessRules
    {
        public Task UserShouldExist([NotNull] Domain.Entities.User? user)
        {
            if (user is null)
                throw new NotFoundException(UserOperationClaimConstants.UserNotFound);

            return Task.CompletedTask;
        }

        public Task OperationClaimShouldExist([NotNull] Domain.Entities.OperationClaim? operationClaim)
        {
            if (operationClaim is null)
                throw new NotFoundException(UserOperationClaimConstants.OperationClaimNotFound);

            return Task.CompletedTask;
        }

        // Aynı yetki iki kez verilemez (ara tablonun birincil anahtarı UserId + OperationClaimId'dir).
        public Task UserShouldNotHaveOperationClaim(Domain.Entities.User user, int operationClaimId)
        {
            if (user.OperationClaims.Any(oc => oc.Id == operationClaimId))
                throw new BusinessException(UserOperationClaimConstants.UserAlreadyHasOperationClaim);

            return Task.CompletedTask;
        }

        public Task UserShouldHaveOperationClaim(Domain.Entities.User user, int operationClaimId)
        {
            if (user.OperationClaims.All(oc => oc.Id != operationClaimId))
                throw new BusinessException(UserOperationClaimConstants.UserDoesNotHaveOperationClaim);

            return Task.CompletedTask;
        }
    }
}
