using ECommerce.Application.Services.Repositories;
using System.Diagnostics.CodeAnalysis;
using Uludag.Core.Exceptions;

namespace ECommerce.Application.Features.Product.Rules
{
    public class ProductBusinessRules : BaseBusinessRules
    {
        private readonly IProductRepository _productRepository;

        public ProductBusinessRules(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // Ürün nesnesi ile ilgili tüm iş kuralları burada yazılır.

        // [NotNull] => metot hata fırlatmadan dönerse "product artık null değil" bilgisini derleyiciye verir.
        // Böylece handler'da ek null kontrolü / "!" gerekmez.
        public Task ProductShouldExistWhenSelected([NotNull] Domain.Entities.Product? product)
        {
            if (product is null)
                throw new NotFoundException("Ürün bulunamadı.");

            return Task.CompletedTask;
        }

        public async Task ProductNameCannotExist(string name)
        {
            // İş kuralı => Sistemin durumuna göre bazen kural geçerli, bazen geçersiz.
            // Validasyon => Sistem durumu gerekmez sadece inputtan geçerli veya geçersiz

            // Ürün ismi 3 harften kısa olamaz. => Validasyon.
            // Ürün ismi zaten var mı? => Sisteme bakmam gerekiyor? DB'de daha önce "ürün 1" ismi var mı? => İş kuralı.

            Domain.Entities.Product? productWithSameName = await _productRepository.GetByNameAsync(name);
            if (productWithSameName != null)
            {
                throw new BusinessException("Product name already exists.");
            }
        }
    }
}
