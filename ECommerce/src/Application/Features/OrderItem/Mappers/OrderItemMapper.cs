using ECommerce.Application.Features.OrderItem.Commands.Create;
using ECommerce.Application.Features.OrderItem.Commands.Update;
using ECommerce.Application.Features.OrderItem.Dtos;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Features.OrderItem.Mappers
{
    // OrderItem ile ilgili tüm dönüşümler (Entity <=> Command / Response) tek bir yerde yapılır;
    // handler'lar dönüşüm yazmaz, yalnızca bu sınıfı çağırır.
    public static class OrderItemMapper
    {
        public static OrderItemResponse ToResponse(this Domain.Entities.OrderItem orderItem)
            => new(
                orderItem.Id,
                orderItem.OrderId,
                orderItem.ProductId,
                orderItem.Quantity,
                orderItem.TotalPrice.Amount,
                orderItem.TotalPrice.Currency);

        public static List<OrderItemResponse> ToResponseList(this IEnumerable<Domain.Entities.OrderItem> orderItems)
            => orderItems.Select(i => i.ToResponse()).ToList();

        // Tutar istemciden alınmaz: ürünün güncel fiyatı * adet olarak hesaplanır.
        // Negatif adet => negatif tutar => Money hata fırlatır.
        public static Domain.Entities.OrderItem MapCreateCommandToOrderItem(CreateOrderItemCommand command, Domain.Entities.Product product) => new()
        {
            OrderId = command.OrderId,
            ProductId = product.Id,
            Quantity = command.Quantity,
            TotalPrice = CalculateTotalPrice(product, command.Quantity)
        };

        // Kalemin siparişi (OrderId) değiştirilemez; ürün ve adet değiştirilebilir.
        public static void MapUpdateCommandToOrderItem(UpdateOrderItemCommand command, Domain.Entities.OrderItem orderItem, Domain.Entities.Product product)
        {
            orderItem.ProductId = product.Id;
            orderItem.Quantity = command.Quantity;
            orderItem.TotalPrice = CalculateTotalPrice(product, command.Quantity);
        }

        private static Money CalculateTotalPrice(Domain.Entities.Product product, int quantity)
            => new(product.Price.Amount * quantity, product.Price.Currency);
    }
}
