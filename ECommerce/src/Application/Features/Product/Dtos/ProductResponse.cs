namespace ECommerce.Application.Features.Product.Dtos
{
    // API'nin dışarıya döndürdüğü ürün modeli.
    // Entity'yi (Domain.Entities.Product) doğrudan döndürmeyiz: veritabanı yapısı değişince
    // API sözleşmesi bozulmasın ve istemciye yalnızca göstermek istediğimiz alanlar gitsin.
    public record ProductResponse(
        Guid Id,
        string? Name,
        decimal Amount,
        string Currency,
        int Stock);
}
