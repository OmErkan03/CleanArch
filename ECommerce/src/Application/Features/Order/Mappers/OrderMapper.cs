using ECommerce.Application.Features.Order.Commands.Create;
using ECommerce.Application.Features.Order.Commands.Update;
using ECommerce.Application.Features.Order.Dtos;
using ECommerce.Application.Features.OrderItem.Mappers;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Features.Order.Mappers
{
    // Order ile ilgili tüm dönüşümler (Entity <=> Command / Response) tek bir yerde yapılır;
    // handler'lar dönüşüm yazmaz, yalnızca bu sınıfı çağırır.
    public static class OrderMapper
    {
        public static OrderResponse ToResponse(this Domain.Entities.Order order)
            => new(
                order.Id,
                order.OrderDate,
                order.TotalPrice.Amount,
                order.TotalPrice.Currency,
                order.Items.ToResponseList());

        public static List<OrderResponse> ToResponseList(this IEnumerable<Domain.Entities.Order> orders)
            => orders.Select(o => o.ToResponse()).ToList();

        // Sipariş boş oluşturulur; kalemler OrderItem endpoint'i ile eklenir ve toplam tutar otomatik hesaplanır.
        public static Domain.Entities.Order MapCreateCommandToOrder(CreateOrderCommand command) => new()
        {
            // Sipariş tarihi istemciden alınmaz, sunucu belirler.
            // PostgreSQL (timestamptz) UTC zaman bekler.
            OrderDate = DateTime.UtcNow,
            // Henüz kalem yok => toplam 0.
            TotalPrice = new Money(0, command.Currency)
        };

        // Yalnızca sipariş tarihi güncellenebilir (bkz. UpdateOrderCommand).
        public static void MapUpdateCommandToOrder(UpdateOrderCommand command, Domain.Entities.Order order)
        {
            // PostgreSQL (timestamptz) UTC zaman bekler; "Z" olmadan gelen tarih yerel saat kabul edilip UTC'ye çevrilir.
            order.OrderDate = command.OrderDate.ToUniversalTime();
        }
    }
}
