using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Services;

public interface IOrderAggregationService
{
    Task<FullOrderDto?> GetFullOrderAsync(long orderId, CancellationToken ct);
}
