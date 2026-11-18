using ECommerce.Application.Features.Order.Dtos;
using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Queries.GetById
{
    // Sipariş bulunamazsa OrderBusinessRules NotFoundException fırlatır (404).
    public class GetByIdOrderQuery : IRequest<OrderResponse>
    {
        public Guid Id { get; set; }

        public class GetByIdOrderQueryHandler : IRequestHandler<GetByIdOrderQuery, OrderResponse>
        {
            private readonly IOrderRepository _orderRepository;
            private readonly OrderBusinessRules _orderBusinessRules;
            public GetByIdOrderQueryHandler(IOrderRepository orderRepository, OrderBusinessRules orderBusinessRules)
            {
                _orderRepository = orderRepository;
                _orderBusinessRules = orderBusinessRules;
            }

            public async Task<OrderResponse> Handle(GetByIdOrderQuery request, CancellationToken cancellationToken)
            {
                var order = await _orderRepository.GetByIdAsync(request.Id, tracking: false, cancellationToken);
                await _orderBusinessRules.OrderShouldExistWhenSelected(order);

                return order.ToResponse();
            }
        }
    }
}
