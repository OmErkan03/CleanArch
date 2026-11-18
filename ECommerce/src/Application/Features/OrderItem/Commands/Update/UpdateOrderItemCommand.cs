using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.OrderItem.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Update
{
    // IRequest => geriye değer döndürmez.
    // Kalem, ürün veya sipariş bulunamazsa NotFoundException (404), para birimi uyuşmazsa BusinessException (400) fırlatılır.
    // Kalemin siparişi (OrderId) değiştirilemez; ürün ve adet değiştirilebilir.
    // TODO: Stok farkı henüz yansıtılmıyor (bilinen risk, ileride eklenecek).
    public class UpdateOrderItemCommand : IRequest, ISecuredRequest
    {
        // Bu işlemi yapabilmek için kullanıcı aşağıdaki claim'lerden EN AZ BİRİNE sahip olmalıdır (bkz. AuthorizationBehavior).
        public string[] Roles => [OrderItemOperationClaims.Admin, OrderItemOperationClaims.Write, OrderItemOperationClaims.Update];

        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }

        public class UpdateOrderItemCommandHandler : IRequestHandler<UpdateOrderItemCommand>
        {
            private readonly IOrderItemRepository _orderItemRepository;
            private readonly IOrderRepository _orderRepository;
            private readonly IProductRepository _productRepository;
            private readonly OrderItemBusinessRules _orderItemBusinessRules;
            private readonly OrderBusinessRules _orderBusinessRules;
            private readonly ProductBusinessRules _productBusinessRules;
            public UpdateOrderItemCommandHandler(
                IOrderItemRepository orderItemRepository,
                IOrderRepository orderRepository,
                IProductRepository productRepository,
                OrderItemBusinessRules orderItemBusinessRules,
                OrderBusinessRules orderBusinessRules,
                ProductBusinessRules productBusinessRules)
            {
                _orderItemRepository = orderItemRepository;
                _orderRepository = orderRepository;
                _productRepository = productRepository;
                _orderItemBusinessRules = orderItemBusinessRules;
                _orderBusinessRules = orderBusinessRules;
                _productBusinessRules = productBusinessRules;
            }

            public async Task Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
            {
                var orderItem = await _orderItemRepository.GetByIdAsync(request.Id, tracking: true, cancellationToken);
                await _orderItemBusinessRules.OrderItemShouldExistWhenSelected(orderItem);

                var product = await _productRepository.GetByIdAsync(request.ProductId, tracking: false, cancellationToken);
                await _productBusinessRules.ProductShouldExistWhenSelected(product);

                // Sipariş kalemleriyle yüklenir; güncellenen kalem zaten takip edildiği için aynı nesne gelir.
                var order = await _orderRepository.GetByIdAsync(orderItem.OrderId, tracking: true, cancellationToken);
                await _orderBusinessRules.OrderShouldExistWhenSelected(order);

                await _orderItemBusinessRules.ProductCurrencyShouldMatchOrderCurrency(product, order);

                OrderItemMapper.MapUpdateCommandToOrderItem(request, orderItem, product);

                order.RecalculateTotalPrice();

                await _orderItemRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
