using ECommerce.Application.Features.Order.Dtos;
using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Queries.GetAll
{
    public class GetAllOrdersQuery : IRequest<List<OrderResponse>>, ISecuredRequest
    {
        // Bu işlemi yapabilmek için kullanıcı aşağıdaki claim'lerden EN AZ BİRİNE sahip olmalıdır (bkz. AuthorizationBehavior).
        public string[] Roles => [OrderOperationClaims.Admin, OrderOperationClaims.Read];

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

                return orders.ToResponseList();
            }
        }
    }
}
