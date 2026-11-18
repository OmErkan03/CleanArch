using ECommerce.Application.Features.Product.Commands.Create;
using ECommerce.Application.Features.Product.Commands.Delete;
using ECommerce.Application.Features.Product.Commands.Update;
using ECommerce.Application.Features.Product.Queries.GetAll;
using ECommerce.Application.Features.Product.Queries.GetById;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var products = await sender.Send(new GetAllProductsQuery(), cancellationToken);

            return Ok(products);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var product = await sender.Send(new GetByIdProductQuery { Id = id }, cancellationToken);

            // Bulunamazsa handler NotFoundException fırlatır => global exception handler 404 döner.
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand createProductCommand, CancellationToken cancellationToken)
        {
            // veritabanı bağlantısı => yanlış
            // iş kodu, validation => yanlış

            // sorumluluk => kullanıcı ile http üzerinden haberleş => isteği doğru handler'a ilet

            // Controller artık handler'ı tanımıyor; isteği mediator'a veriyor, mediator doğru handler'ı buluyor.
            var id = await sender.Send(createProductCommand, cancellationToken);

            // 201 Created + "Location: /api/products/{id}" başlığı => oluşan ürünün adresini istemciye bildirir.
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductCommand updateProductCommand, CancellationToken cancellationToken)
        {
            // Id route'tan gelir; body'deki değer ne olursa olsun URL'deki Id esas alınır.
            updateProductCommand.Id = id;
            await sender.Send(updateProductCommand, cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            await sender.Send(new DeleteProductCommand { Id = id }, cancellationToken);

            return NoContent();
        }
    }
}
