using ECommerce.Application.Features.UserOperationClaim.Notifications;
using ECommerce.Application.Features.UserOperationClaim.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.Constants;

namespace ECommerce.Application.Features.UserOperationClaim.Commands.Revoke
{
    // Kullanıcıdan bir yetkiyi geri alır. Kullanıcının elindeki token'ın süresi dolmamış olsa bile
    // yetki bir sonraki isteğinde artık geçmez (oturum Redis'te güncellenir).
    public class RevokeOperationClaimCommand : IRequest, ISecuredRequest
    {
        public string[] Roles => [GeneralOperationClaims.Admin];

        public Guid UserId { get; set; }
        public int OperationClaimId { get; set; }

        public class RevokeOperationClaimCommandHandler : IRequestHandler<RevokeOperationClaimCommand>
        {
            private readonly IUserRepository _userRepository;
            private readonly UserOperationClaimBusinessRules _rules;
            private readonly IPublisher _publisher;

            public RevokeOperationClaimCommandHandler(
                IUserRepository userRepository,
                UserOperationClaimBusinessRules rules,
                IPublisher publisher)
            {
                _userRepository = userRepository;
                _rules = rules;
                _publisher = publisher;
            }

            public async Task Handle(RevokeOperationClaimCommand request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetByIdWithOperationClaimsAsync(request.UserId, cancellationToken);
                await _rules.UserShouldExist(user);

                await _rules.UserShouldHaveOperationClaim(user, request.OperationClaimId);

                // Koleksiyondan çıkarmak, SaveChanges'te ara tablodan (UserOperationClaims) DELETE olarak yansır.
                var operationClaim = user.OperationClaims.First(oc => oc.Id == request.OperationClaimId);
                user.OperationClaims.Remove(operationClaim);
                await _userRepository.SaveChangesAsync(cancellationToken);

                await _publisher.Publish(new UserOperationClaimsChangedNotification { UserId = user.Id }, cancellationToken);
            }
        }
    }
}
