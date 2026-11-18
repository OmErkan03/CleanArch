using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Order.Commands.Create
{
    // IRequest<Guid> => oluşturulan siparişin Id'sini döndürür.
    // Sipariş boş oluşturulur; kalemler OrderItem endpoint'i ile eklenir ve toplam tutar otomatik hesaplanır.
    public class CreateOrderCommand : IRequest<Guid>, ISecuredRequest
    {
        // Bu işlemi yapabilmek için kullanıcı aşağıdaki claim'lerden EN AZ BİRİNE sahip olmalıdır (bkz. AuthorizationBehavior).
        public string[] Roles => [OrderOperationClaims.Admin, OrderOperationClaims.Write, OrderOperationClaims.Create];

        // Siparişin para birimi. Siparişe eklenen tüm ürünler bu para biriminde olmalıdır.
        public string Currency { get; set; } = "TRY";

        public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
        {
            private readonly IOrderRepository _orderRepository;
            public CreateOrderCommandHandler(IOrderRepository orderRepository)
            {
                _orderRepository = orderRepository;
            }

            public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
            {
                var order = OrderMapper.MapCreateCommandToOrder(request);

                await _orderRepository.AddAsync(order, cancellationToken);
                await _orderRepository.SaveChangesAsync(cancellationToken);

                return order.Id;
            }
        }
    }
}
