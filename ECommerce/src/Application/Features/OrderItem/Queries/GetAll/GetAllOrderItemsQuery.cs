using ECommerce.Application.Features.OrderItem.Dtos;
using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Queries.GetAll
{
    public class GetAllOrderItemsQuery : IRequest<List<OrderItemResponse>>
    {
        public class GetAllOrderItemsQueryHandler : IRequestHandler<GetAllOrderItemsQuery, List<OrderItemResponse>>
        {
            private readonly IOrderItemRepository _orderItemRepository;
            public GetAllOrderItemsQueryHandler(IOrderItemRepository orderItemRepository)
            {
                _orderItemRepository = orderItemRepository;
            }

            public async Task<List<OrderItemResponse>> Handle(GetAllOrderItemsQuery request, CancellationToken cancellationToken)
            {
                var orderItems = await _orderItemRepository.GetAllAsync(cancellationToken);

                return orderItems.Select(i => i.ToResponse()).ToList();
            }
        }
    }
}
