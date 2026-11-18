using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Update
{
    // IRequest => geriye değer döndürmez. Sipariş bulunamazsa OrderBusinessRules NotFoundException fırlatır (404).
    // Yalnızca sipariş tarihi güncellenebilir: toplam tutar kalemlerden hesaplanır,
    // para birimi de mevcut kalemlerle uyumsuz hale gelmesin diye değiştirilemez.
    public class UpdateOrderCommand : IRequest, ISecuredRequest
    {
        // Bu işlemi yapabilmek için kullanıcı aşağıdaki claim'lerden EN AZ BİRİNE sahip olmalıdır (bkz. AuthorizationBehavior).
        public string[] Roles => [OrderOperationClaims.Admin, OrderOperationClaims.Write, OrderOperationClaims.Update];

        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }

        public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
        {
            private readonly IOrderRepository _orderRepository;
            private readonly OrderBusinessRules _orderBusinessRules;
            public UpdateOrderCommandHandler(IOrderRepository orderRepository, OrderBusinessRules orderBusinessRules)
            {
                _orderRepository = orderRepository;
                _orderBusinessRules = orderBusinessRules;
            }

            public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
            {
                var order = await _orderRepository.GetByIdAsync(request.Id, tracking: true, cancellationToken);
                await _orderBusinessRules.OrderShouldExistWhenSelected(order);

                OrderMapper.MapUpdateCommandToOrder(request, order);

                await _orderRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
