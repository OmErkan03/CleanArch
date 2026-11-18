using ECommerce.Application.Features.OrderItem.Commands.Create;
using ECommerce.Application.Features.OrderItem.Commands.Delete;
using ECommerce.Application.Features.OrderItem.Commands.Update;
using ECommerce.Application.Features.OrderItem.Queries.GetAll;
using ECommerce.Application.Features.OrderItem.Queries.GetById;
using Microsoft.AspNetCore.Mvc;
using Uludag.Core.Mediation;

namespace ECommerce.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemsController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var orderItems = await sender.Send(new GetAllOrderItemsQuery(), cancellationToken);

            return Ok(orderItems);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var orderItem = await sender.Send(new GetByIdOrderItemQuery { Id = id }, cancellationToken);

            // Bulunamazsa handler NotFoundException fırlatır => global exception handler 404 döner.
            return Ok(orderItem);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderItemCommand createOrderItemCommand, CancellationToken cancellationToken)
        {
            var id = await sender.Send(createOrderItemCommand, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateOrderItemCommand updateOrderItemCommand, CancellationToken cancellationToken)
        {
            updateOrderItemCommand.Id = id;
            await sender.Send(updateOrderItemCommand, cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            await sender.Send(new DeleteOrderItemCommand { Id = id }, cancellationToken);

            return NoContent();
        }
    }
}
