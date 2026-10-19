using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Orders.Bll.Dtos;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> GetByCustomer(
        [FromQuery, Range(1, long.MaxValue)] long customerId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await _orders.GetByCustomerAsync(customerId, status, ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
        => Ok(await _orders.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto input, CancellationToken ct)
    {
        var created = await _orders.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:long}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await _orders.ConfirmAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/status")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> ChangeStatus(long id, ChangeOrderStatusDto input, CancellationToken ct)
        => Ok(await _orders.ChangeStatusAsync(id, input, ct));

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _orders.DeleteAsync(id, ct);
        return NoContent();
    }
}
