using ECommerce.Application.Features.Order.Mappers;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Order.Constants;
using Uludag.Core.Mediation;
using Uludag.Core.Security;

namespace ECommerce.Application.Features.Order.Commands.Create
{
    // IRequest<Guid> => oluşturulan siparişin Id'sini döndürür.
    // Sipariş, isteği yapan (oturum açmış) kullanıcıya ait olarak oluşturulur.
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
            private readonly ICurrentUser _currentUser;
            public CreateOrderCommandHandler(IOrderRepository orderRepository, ICurrentUser currentUser)
            {
                _orderRepository = orderRepository;
                _currentUser = currentUser;
            }

            public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
            {
                // ISecuredRequest olduğu için kimliği doğrulanmamış istek buraya ulaşmaz; UserId her zaman doludur.
                var order = OrderMapper.MapCreateCommandToOrder(request, _currentUser.UserId!.Value);

                await _orderRepository.AddAsync(order, cancellationToken);
                await _orderRepository.SaveChangesAsync(cancellationToken);

                return order.Id;
            }
        }
    }
}
