using ECommerce.Application.Features.Product.Mappers;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Update
{
    // IRequest => geriye değer döndürmez. Ürün bulunamazsa ProductBusinessRules NotFoundException fırlatır (404).
    public class UpdateProductCommand : IRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int Stock { get; set; }

        public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
        {
            private readonly IProductRepository _productRepository;
            private readonly ProductBusinessRules _productBusinessRules;
            public UpdateProductCommandHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
            {
                _productRepository = productRepository;
                _productBusinessRules = productBusinessRules;
            }

            public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
            {
                // tracking: true => nesne EF tarafından takip edilir, değişiklikler SaveChanges'te yazılır.
                var product = await _productRepository.GetByIdAsync(request.Id, tracking: true, cancellationToken);
                await _productBusinessRules.ProductShouldExistWhenSelected(product);

                // Bulunan nesne EF tarafından takip edilir; alanlarını değiştirmek yeterli,
                // SaveChanges yalnızca değişen kolonlar için UPDATE üretir.
                ProductMapper.MapUpdateCommandToProduct(request, product);

                await _productRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
