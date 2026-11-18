using ECommerce.Application.Features.Product.Dtos;

namespace ECommerce.Application.Features.Product.Mappers
{
    // Entity => Response dönüşümü tek bir yerde yapılır; her handler kendi dönüşümünü yazmaz.
    public static class ProductMapper
    {
        public static ProductResponse ToResponse(this Domain.Entities.Product product)
            => new(
                product.Id,
                product.Name,
                product.Price.Amount,
                product.Price.Currency,
                product.Stock);
    }
}
