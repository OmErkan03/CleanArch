using ECommerce.Domain.ValueObjects;
using ECommerce.Application.Features.Order.Rules;
using ECommerce.Application.Features.OrderItem.Rules;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.OrderItem.Commands.Create
{
    // IRequest<Guid> => oluşturulan kalemin Id'si.
    // Sipariş veya ürün bulunamazsa NotFoundException (404), para birimi uyuşmazsa BusinessException (400) fırlatılır.
    // Tutar istemciden alınmaz: ürünün güncel fiyatı * adet olarak hesaplanır.
    // TODO: Stok düşümü henüz yapılmıyor (bilinen risk, ileride eklenecek).
    public class CreateOrderItemCommand : IRequest<Guid>
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }

        public class CreateOrderItemCommandHandler : IRequestHandler<CreateOrderItemCommand, Guid>
        {
            private readonly IOrderItemRepository _orderItemRepository;
            private readonly IOrderRepository _orderRepository;
            private readonly IProductRepository _productRepository;
            private readonly OrderItemBusinessRules _orderItemBusinessRules;
            private readonly OrderBusinessRules _orderBusinessRules;
            private readonly ProductBusinessRules _productBusinessRules;
            public CreateOrderItemCommandHandler(
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

            public async Task<Guid> Handle(CreateOrderItemCommand request, CancellationToken cancellationToken)
            {
                // Sipariş kalemleriyle birlikte, takip edilerek yüklenir (toplam tutar güncellenecek).
                var order = await _orderRepository.GetByIdAsync(request.OrderId, tracking: true, cancellationToken);
                await _orderBusinessRules.OrderShouldExistWhenSelected(order);

                // Üründen yalnızca fiyat okunur => takip gereksiz.
                var product = await _productRepository.GetByIdAsync(request.ProductId, tracking: false, cancellationToken);
                await _productBusinessRules.ProductShouldExistWhenSelected(product);

                await _orderItemBusinessRules.ProductCurrencyShouldMatchOrderCurrency(product, order);

                Domain.Entities.OrderItem orderItem = new()
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = request.Quantity,
                    // Negatif adet => negatif tutar => Money hata fırlatır.
                    TotalPrice = new Money(product.Price.Amount * request.Quantity, product.Price.Currency)
                };

                // EF, takip edilen siparişin Items koleksiyonuna yeni kalemi otomatik ekler (navigation fixup).
                await _orderItemRepository.AddAsync(orderItem, cancellationToken);
                order.RecalculateTotalPrice();

                // Aynı DbContext paylaşıldığı için tek SaveChanges hem INSERT'i hem UPDATE'i yazar (tek transaction).
                await _orderItemRepository.SaveChangesAsync(cancellationToken);

                return orderItem.Id;
            }
        }
    }
}
