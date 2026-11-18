using ECommerce.Application.Features.Order.Commands.Create;
using ECommerce.Application.Features.Order.Commands.Delete;
using ECommerce.Application.Features.Order.Commands.Update;
using ECommerce.Application.Features.Order.Queries.GetAll;
using ECommerce.Application.Features.Order.Queries.GetById;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var orders = await sender.Send(new GetAllOrdersQuery(), cancellationToken);

            return Ok(orders);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var order = await sender.Send(new GetByIdOrderQuery { Id = id }, cancellationToken);

            // Bulunamazsa handler NotFoundException fırlatır => global exception handler 404 döner.
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderCommand createOrderCommand, CancellationToken cancellationToken)
        {
            var id = await sender.Send(createOrderCommand, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateOrderCommand updateOrderCommand, CancellationToken cancellationToken)
        {
            // Id route'tan gelir; body'deki değer ne olursa olsun URL'deki Id esas alınır.
            updateOrderCommand.Id = id;
            await sender.Send(updateOrderCommand, cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            await sender.Send(new DeleteOrderCommand { Id = id }, cancellationToken);

            return NoContent();
        }
    }
}
