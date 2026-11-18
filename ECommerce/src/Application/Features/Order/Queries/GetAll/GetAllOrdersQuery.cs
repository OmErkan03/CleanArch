using ECommerce.Application.Features.Order.Dtos;
using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Queries.GetAll
{
    public class GetAllOrdersQuery : IRequest<List<OrderResponse>>
    {
        public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, List<OrderResponse>>
        {
            private readonly IOrderRepository _orderRepository;
            public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
            {
                _orderRepository = orderRepository;
            }

            public async Task<List<OrderResponse>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
            {
                var orders = await _orderRepository.GetAllAsync(cancellationToken);

                return orders.Select(o => o.ToResponse()).ToList();
            }
        }
    }
}
