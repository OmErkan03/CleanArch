using ECommerce.Application.Features.Product.Dtos;
using ECommerce.Application.Features.Product.Mappers;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Queries.GetAll
{
    // Query => veriyi yalnızca okur, hiçbir şeyi değiştirmez (Command'in tersi).
    // Parametre almadığı için gövdesi boş; ileride sayfalama/filtre alanları buraya eklenir.
    public class GetAllProductsQuery : IRequest<List<ProductResponse>>
    {
        public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, List<ProductResponse>>
        {
            private readonly IProductRepository _productRepository;
            public GetAllProductsQueryHandler(IProductRepository productRepository)
            {
                _productRepository = productRepository;
            }

            public async Task<List<ProductResponse>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
            {
                // Repository okuma işlemini AsNoTracking ile yapar (Query'lerde takip gereksizdir).
                var products = await _productRepository.GetAllAsync(cancellationToken);

                return products.ToResponseList();
            }
        }
    }
}
