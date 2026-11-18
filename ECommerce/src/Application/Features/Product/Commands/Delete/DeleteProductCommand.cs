using ECommerce.Application.Features.Product.Rules;
using ECommerce.Application.Services.Repositories;
using ECommerce.Application.Features.Product.Constants;
using Uludag.Core.Mediation;

namespace ECommerce.Application.Features.Product.Commands.Delete
{
    // IRequest => geriye değer döndürmez. Ürün bulunamazsa ProductBusinessRules NotFoundException fırlatır (404).
    public class DeleteProductCommand : IRequest, ISecuredRequest
    {
        // Bu işlemi yapabilmek için kullanıcı aşağıdaki claim'lerden EN AZ BİRİNE sahip olmalıdır (bkz. AuthorizationBehavior).
        public string[] Roles => [ProductOperationClaims.Admin, ProductOperationClaims.Write, ProductOperationClaims.Delete];

        public Guid Id { get; set; }

        public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
        {
            private readonly IProductRepository _productRepository;
            private readonly ProductBusinessRules _productBusinessRules;
            public DeleteProductCommandHandler(IProductRepository productRepository, ProductBusinessRules productBusinessRules)
            {
                _productRepository = productRepository;
                _productBusinessRules = productBusinessRules;
            }
            public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
            {
                var product = await _productRepository.GetByIdAsync(request.Id, tracking: true, cancellationToken);
                await _productBusinessRules.ProductShouldExistWhenSelected(product);

                // Remove => nesneyi "silinecek" olarak işaretler, DELETE sorgusu SaveChanges'te çalışır.
                _productRepository.Remove(product);
                await _productRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
