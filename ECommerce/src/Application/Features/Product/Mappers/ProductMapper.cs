using ECommerce.Application.Features.Product.Commands.Create;
using ECommerce.Application.Features.Product.Commands.Update;
using ECommerce.Application.Features.Product.Dtos;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Features.Product.Mappers
{
    // Product ile ilgili tüm dönüşümler (Entity <=> Command / Response) tek bir yerde yapılır;
    // handler'lar dönüşüm yazmaz, yalnızca bu sınıfı çağırır.
    public static class ProductMapper
    {
        public static ProductResponse ToResponse(this Domain.Entities.Product product)
            => new(
                product.Id,
                product.Name,
                product.Price.Amount,
                product.Price.Currency,
                product.Stock);

        public static List<ProductResponse> ToResponseList(this IEnumerable<Domain.Entities.Product> products)
            => products.Select(p => p.ToResponse()).ToList();

        public static Domain.Entities.Product MapCreateCommandToProduct(CreateProductCommand command) => new()
        {
            Name = command.Name,
            Price = new Money(command.Amount, command.Currency),
            Stock = command.Stock
        };

        // Var olan (EF tarafından takip edilen) ürünün alanlarını komuttaki değerlerle günceller.
        // Yeni nesne oluşturulmaz; SaveChanges yalnızca değişen kolonlar için UPDATE üretir.
        public static void MapUpdateCommandToProduct(UpdateProductCommand command, Domain.Entities.Product product)
        {
            product.Name = command.Name;
            product.Stock = command.Stock;
            // Money bir Value Object: alanları tek tek değiştirilmez, yenisi oluşturulur.
            product.Price = new Money(command.Amount, command.Currency);
        }
    }
}
