using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Contracts;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public sealed class AggregatorController : ControllerBase
{
    private readonly IOrderAggregationService _aggregation;

    public AggregatorController(IOrderAggregationService aggregation)
    {
        _aggregation = aggregation;
    }

    [HttpGet("orders/{id:long}/full")]
    [ProducesResponseType<FullOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FullOrderDto>> GetFullOrder(long id, CancellationToken ct)
    {
        try
        {
            var result = await _aggregation.GetFullOrderAsync(id, ct);
            return result is null
                ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Order not found",
                    detail: $"Order {id} was not found")
                : Ok(result);
        }
        catch (DependencyUnavailableException ex)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Required service is unavailable", detail: ex.Message);
        }
    }
}
