using ECommerce.Application.Features.UserOperationClaim.Notifications;
using ECommerce.Application.Features.UserOperationClaim.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using Uludag.Core.Security.Constants;

namespace ECommerce.Application.Features.UserOperationClaim.Commands.Assign
{
    // Kullanıcıya bir yetki (operation claim) verir. Değişiklik kullanıcının açık oturumlarına ANINDA yansır.
    public class AssignOperationClaimCommand : IRequest, ISecuredRequest
    {
        // Yetki dağıtmak sistemin en hassas işlemidir => yalnızca Admin.
        public string[] Roles => [GeneralOperationClaims.Admin];

        public Guid UserId { get; set; }
        public int OperationClaimId { get; set; }

        public class AssignOperationClaimCommandHandler : IRequestHandler<AssignOperationClaimCommand>
        {
            private readonly IUserRepository _userRepository;
            private readonly IOperationClaimRepository _operationClaimRepository;
            private readonly UserOperationClaimBusinessRules _rules;
            private readonly IPublisher _publisher;

            public AssignOperationClaimCommandHandler(
                IUserRepository userRepository,
                IOperationClaimRepository operationClaimRepository,
                UserOperationClaimBusinessRules rules,
                IPublisher publisher)
            {
                _userRepository = userRepository;
                _operationClaimRepository = operationClaimRepository;
                _rules = rules;
                _publisher = publisher;
            }

            public async Task Handle(AssignOperationClaimCommand request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetByIdWithOperationClaimsAsync(request.UserId, cancellationToken);
                await _rules.UserShouldExist(user);

                var operationClaim = await _operationClaimRepository.GetByIdAsync(request.OperationClaimId, cancellationToken);
                await _rules.OperationClaimShouldExist(operationClaim);

                await _rules.UserShouldNotHaveOperationClaim(user, request.OperationClaimId);

                // Koleksiyona eklemek, SaveChanges'te ara tabloya (UserOperationClaims) INSERT olarak yansır.
                user.OperationClaims.Add(operationClaim);
                await _userRepository.SaveChangesAsync(cancellationToken);

                // Veritabanı güncellendi => kullanıcının Redis'teki oturumları da güncellensin.
                await _publisher.Publish(new UserOperationClaimsChangedNotification { UserId = user.Id }, cancellationToken);
            }
        }
    }
}
