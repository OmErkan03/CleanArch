using System.Diagnostics.CodeAnalysis;
using ECommerce.Application.Features.User.Constants;
using Uludag.Core.Exceptions;

namespace ECommerce.Application.Features.User.Rules
{
    // Kullanıcı yönetimi (Admin işlemleri) ile ilgili iş kuralları.
    public class UserBusinessRules : BaseBusinessRules
    {
        public Task UserShouldExist([NotNull] Domain.Entities.User? user)
        {
            if (user is null)
                throw new NotFoundException(UserConstants.UserNotFound);

            return Task.CompletedTask;
        }

        public Task MfaShouldBeEnabled(Domain.Entities.User user)
        {
            if (!user.IsMfaEnabled)
                throw new BusinessException(UserConstants.MfaNotEnabled);

            return Task.CompletedTask;
        }
    }
}
