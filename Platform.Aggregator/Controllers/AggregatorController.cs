using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Contracts;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public sealed class AggregatorController : ControllerBase
{
    private readonly GrpcOrderAggregationService _grpc;
    private readonly HttpOrderAggregationService _http;

    public AggregatorController(GrpcOrderAggregationService grpc, HttpOrderAggregationService http)
    {
        _grpc = grpc;
        _http = http;
    }

    [HttpGet("orders/{id:long}/full")]
    [ProducesResponseType<FullOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public Task<ActionResult<FullOrderDto>> GetFullOrder(long id, CancellationToken ct)
        => RunAsync(id, () => _grpc.GetFullOrderAsync(id, ct));

    [HttpGet("orders/{id:long}/full/grpc")]
    [ProducesResponseType<FullOrderDto>(StatusCodes.Status200OK)]
    public Task<ActionResult<FullOrderDto>> GetFullOrderGrpcWithoutCache(long id, CancellationToken ct)
        => RunAsync(id, () => _grpc.AggregateAsync(id, ct));

    [HttpGet("orders/{id:long}/full/http")]
    [ProducesResponseType<FullOrderDto>(StatusCodes.Status200OK)]
    public Task<ActionResult<FullOrderDto>> GetFullOrderHttp(long id, CancellationToken ct)
        => RunAsync(id, () => _http.GetFullOrderAsync(id, ct));

    private async Task<ActionResult<FullOrderDto>> RunAsync(long id, Func<Task<FullOrderDto?>> aggregate)
    {
        try
        {
            var result = await aggregate();
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
