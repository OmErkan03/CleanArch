using ECommerce.Application.Features.Product.Dtos;
using ECommerce.Application.Features.Product.Mappers;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Queries.GetById
{
    // Ürün bulunamazsa ProductBusinessRules NotFoundException fırlatır (404).
    public class GetByIdProductQuery : IRequest<ProductResponse>
    {
        public Guid Id { get; set; }

        public class GetByIdProductQueryHandler : IRequestHandler<GetByIdProductQuery, ProductResponse>
        {
            private readonly IProductRepository _productRepository;
            private readonly ProductBusinessRules _productBusinessRules;
            public GetByIdProductQueryHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
            {
                _productRepository = productRepository;
                _productBusinessRules = productBusinessRules;
            }

            public async Task<ProductResponse> Handle(GetByIdProductQuery request, CancellationToken cancellationToken)
            {
                // tracking: false => sadece okuma yapıyoruz, EF'in nesneyi takip etmesine gerek yok.
                var product = await _productRepository.GetByIdAsync(request.Id, tracking: false, cancellationToken);
                await _productBusinessRules.ProductShouldExistWhenSelected(product);

                return product.ToResponse();
            }
        }
    }
}
