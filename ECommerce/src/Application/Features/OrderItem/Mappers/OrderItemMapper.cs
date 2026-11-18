using ECommerce.Application.Features.OrderItem.Dtos;

namespace ECommerce.Application.Features.OrderItem.Mappers
{
    // Entity => Response dönüşümü tek bir yerde yapılır.
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
    }
}
