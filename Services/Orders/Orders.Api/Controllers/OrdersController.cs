using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Orders.Api.Authorization;
using Orders.Api.Metrics;
using Orders.Bll.Dtos;
using Orders.Bll.Services;
using Platform.ServiceDefaults.Security;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly OrderMetrics _metrics;
    private readonly string _authServiceIssuer;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orders, OrderMetrics metrics, IConfiguration configuration,
        ILogger<OrdersController> logger)
    {
        _orders = orders;
        _metrics = metrics;
        _authServiceIssuer = PlatformAuthentication.AuthServiceIssuer(configuration);
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission(Permissions.Orders.Read)]
    [ProducesResponseType<IReadOnlyList<OrderSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> GetByCustomer(
        [FromQuery, Range(1, long.MaxValue)] long customerId, [FromQuery] string? status, CancellationToken ct)
    {
        if (!CustomerAccess.CanAccess(User, customerId, _authServiceIssuer))
            return ForeignCustomer(customerId);

        return Ok(await _orders.GetByCustomerAsync(customerId, status, ct));
    }

    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Orders.Read)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
    {
        var order = await _metrics.MeasureAsync("get", () => _orders.GetByIdAsync(id, ct));

        if (!CustomerAccess.CanAccess(User, order.CustomerId, _authServiceIssuer))
            return ForeignCustomer(order.CustomerId);

        return Ok(order);
    }

    [HttpPost]
    [RequirePermission(Permissions.Orders.Create)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto input, CancellationToken ct)
    {
        if (!CustomerAccess.CanAccess(User, input.CustomerId, _authServiceIssuer))
            return ForeignCustomer(input.CustomerId);

        var created = await _metrics.MeasureAsync("create", () => _orders.CreateAsync(input, ct));
        _metrics.OrderCreated(created.Status);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.Orders.Update)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Update(long id, UpdateOrderDto input, CancellationToken ct)
        => Ok(await _metrics.MeasureAsync("update", () => _orders.UpdateDetailsAsync(id, input, ct)));

    /// <summary>Підтвердження замовлення (approve). Стара адреса /confirm з Лаб. 02 залишена як синонім.</summary>
    [HttpPost("{id:long}/approve")]
    [HttpPost("{id:long}/confirm")]
    [RequirePermission(Permissions.Orders.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        await _metrics.MeasureAsync("confirm", () => _orders.ConfirmAsync(id, ct));
        _metrics.OrderConfirmed();
        return NoContent();
    }

    [HttpPost("{id:long}/status")]
    [RequirePermission(Permissions.Orders.Update)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> ChangeStatus(long id, ChangeOrderStatusDto input, CancellationToken ct)
    {
        var order = await _metrics.MeasureAsync("change_status", () => _orders.ChangeStatusAsync(id, input, ct));
        _metrics.StatusChanged(order.Status);
        return Ok(order);
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Orders.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _metrics.MeasureAsync("delete", () => _orders.DeleteAsync(id, ct));
        _metrics.OrderDeleted();
        return NoContent();
    }

    private ObjectResult ForeignCustomer(long customerId)
    {
        _logger.LogWarning("Subject {Subject} denied access to orders of customer {CustomerId}",
            User.FindFirst(PlatformAuthentication.SubjectClaimType)?.Value, customerId);

        return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden",
            detail: "You can access only the orders of your own customer account");
    }
}
