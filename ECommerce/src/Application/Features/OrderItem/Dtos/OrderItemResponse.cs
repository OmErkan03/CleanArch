namespace ECommerce.Application.Features.OrderItem.Dtos
{
    // API'nin dışarıya döndürdüğü sipariş kalemi modeli.
    public record OrderItemResponse(
        Guid Id,
        Guid OrderId,
        Guid ProductId,
        int Quantity,
        decimal Amount,
        string Currency);
}
