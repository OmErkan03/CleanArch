using ECommerce.Application.Features.OrderItem.Dtos;
using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Queries.GetById
{
    // Kalem bulunamazsa OrderItemBusinessRules NotFoundException fırlatır (404).
    public class GetByIdOrderItemQuery : IRequest<OrderItemResponse>
    {
        public Guid Id { get; set; }

        public class GetByIdOrderItemQueryHandler : IRequestHandler<GetByIdOrderItemQuery, OrderItemResponse>
        {
            private readonly IOrderItemRepository _orderItemRepository;
            private readonly OrderItemBusinessRules _orderItemBusinessRules;
            public GetByIdOrderItemQueryHandler(IOrderItemRepository orderItemRepository, OrderItemBusinessRules orderItemBusinessRules)
            {
                _orderItemRepository = orderItemRepository;
                _orderItemBusinessRules = orderItemBusinessRules;
            }

            public async Task<OrderItemResponse> Handle(GetByIdOrderItemQuery request, CancellationToken cancellationToken)
            {
                var orderItem = await _orderItemRepository.GetByIdAsync(request.Id, tracking: false, cancellationToken);
                await _orderItemBusinessRules.OrderItemShouldExistWhenSelected(orderItem);

                return orderItem.ToResponse();
            }
        }
    }
}
