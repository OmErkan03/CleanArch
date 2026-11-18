using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Delete
{
    // IRequest => geriye değer döndürmez. Kalem bulunamazsa NotFoundException fırlatılır (404).
    public class DeleteOrderItemCommand : IRequest
    {
        public Guid Id { get; set; }

        public class DeleteOrderItemCommandHandler : IRequestHandler<DeleteOrderItemCommand>
        {
            private readonly IOrderItemRepository _orderItemRepository;
            private readonly IOrderRepository _orderRepository;
            private readonly OrderItemBusinessRules _orderItemBusinessRules;
            private readonly OrderBusinessRules _orderBusinessRules;
            public DeleteOrderItemCommandHandler(
                IOrderItemRepository orderItemRepository,
                IOrderRepository orderRepository,
                OrderItemBusinessRules orderItemBusinessRules,
                OrderBusinessRules orderBusinessRules)
            {
                _orderItemRepository = orderItemRepository;
                _orderRepository = orderRepository;
                _orderItemBusinessRules = orderItemBusinessRules;
                _orderBusinessRules = orderBusinessRules;
            }

            public async Task Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
            {
                var orderItem = await _orderItemRepository.GetByIdAsync(request.Id, tracking: true, cancellationToken);
                await _orderItemBusinessRules.OrderItemShouldExistWhenSelected(orderItem);

                // Kalem zorunlu bir FK ile siparişe bağlıdır; yine de kontrol kural sınıfında yapılır.
                var order = await _orderRepository.GetByIdAsync(orderItem.OrderId, tracking: true, cancellationToken);
                await _orderBusinessRules.OrderShouldExistWhenSelected(order);

                _orderItemRepository.Remove(orderItem);

                // Silinen kalem SaveChanges'e kadar koleksiyonda kalır; toplamdan düşmek için çıkarıyoruz.
                order.Items.Remove(orderItem);
                order.RecalculateTotalPrice();

                await _orderItemRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
