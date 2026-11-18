using ECommerce.Domain.ValueObjects;
using ECommerce.Application.Services.Repositories;
using Uludag.Core.Mediation;
using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Features.Product.Mappers;

namespace ECommerce.Application.Features.Product.Commands.Create
{
    // IRequest<Guid> => bu bir istektir ve geriye oluşturulan ürünün Id'sini döndürür.
    // (Id dönmezse, istemci oluşturduğu ürünü güncellemek/silmek için Id'yi bilemez.)
    public class CreateProductCommand : IRequest<Guid> // Komut varolmadan
    {
        // İki parçadan oluşur => Komutun Gövdesi, (Create işlemi yapılırken ne istiyorsun?)
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int Stock { get; set; }

        // İkinci parça => Command'in Handler'ı

        // IRequestHandler<CreateProductCommand, Guid> => Mediator, CreateProductCommand geldiğinde bu sınıfı bulup çalıştırır.
        // Handler'ı controller "new"lemez; DI container'a otomatik kaydedilir (MediatorRegistrar assembly'yi tarar).
        public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid> // Komut Handler'ı, işleyen kişi
        {
            // Handler DbContext'i değil, repository arayüzünü bilir (Repository Pattern).
            private readonly IProductRepository _productRepository;
            private readonly ProductBusinessRules productBusinessRules;
            public CreateProductCommandHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
            {
                _productRepository = productRepository;
                this.productBusinessRules = productBusinessRules;
            }
            // Orchestrator gibi davranır.
            // Doğru sırayla, doğru kişileri çağırır. (BusinessRules, Repository)
            // İş kuralı
            public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
            {
                await productBusinessRules.ProductNameCannotExist(request.Name);
                
                // İşi kendisi yapıyor.
                // Mapper => gibi davrandı.

                var product = ProductMapper.MapCreateCommandToProduct(request);

                // AddAsync sırasında EF, Guid tipindeki Id'yi otomatik üretir.
                await _productRepository.AddAsync(product, cancellationToken);
                await _productRepository.SaveChangesAsync(cancellationToken);

                return product.Id;
            }
        }
    }
}
