using ECommerce.Application.Features.OrderItem.Dtos;
using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.OrderItem.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Queries.GetAll
{
    public class GetAllOrderItemsQuery : IRequest<List<OrderItemResponse>>, ISecuredRequest
    {
        // Bu işlemi yapabilmek için kullanıcı aşağıdaki claim'lerden EN AZ BİRİNE sahip olmalıdır (bkz. AuthorizationBehavior).
        public string[] Roles => [OrderItemOperationClaims.Admin, OrderItemOperationClaims.Read];

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

                return orderItems.ToResponseList();
            }
        }
    }
}
