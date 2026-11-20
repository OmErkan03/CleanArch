using ECommerce.Application.Features.OrderItem.Dtos;

namespace ECommerce.Application.Features.Order.Dtos
{
    // API'nin dışarıya döndürdüğü sipariş modeli; kalemleri de içerir.
    public record OrderResponse(
        Guid Id,
        Guid UserId,
        DateTime OrderDate,
        decimal Amount,
        string Currency,
        List<OrderItemResponse> Items);
}
