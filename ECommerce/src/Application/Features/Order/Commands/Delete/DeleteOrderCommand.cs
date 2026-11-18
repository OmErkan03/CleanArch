using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Delete
{
    // IRequest => geriye değer döndürmez. Sipariş bulunamazsa OrderBusinessRules NotFoundException fırlatır (404).
    public class DeleteOrderCommand : IRequest
    {
        public Guid Id { get; set; }

        public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand>
        {
            private readonly IOrderRepository _orderRepository;
            private readonly OrderBusinessRules _orderBusinessRules;
            public DeleteOrderCommandHandler(IOrderRepository orderRepository, OrderBusinessRules orderBusinessRules)
            {
                _orderRepository = orderRepository;
                _orderBusinessRules = orderBusinessRules;
            }

            public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
            {
                var order = await _orderRepository.GetByIdAsync(request.Id, tracking: true, cancellationToken);
                await _orderBusinessRules.OrderShouldExistWhenSelected(order);

                // Kalemler de silinir (Cascade Delete, bkz. OrderItemConfigurations).
                _orderRepository.Remove(order);
                await _orderRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
