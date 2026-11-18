using ECommerce.Application.Features.Order.Dtos;
using ECommerce.Application.Features.OrderItem.Mappers;

namespace ECommerce.Application.Features.Order.Mappers
{
    // Entity => Response dönüşümü tek bir yerde yapılır.
    public static class OrderMapper
    {
        public static OrderResponse ToResponse(this Domain.Entities.Order order)
            => new(
                order.Id,
                order.OrderDate,
                order.TotalPrice.Amount,
                order.TotalPrice.Currency,
                order.Items.Select(i => i.ToResponse()).ToList());
    }
}
