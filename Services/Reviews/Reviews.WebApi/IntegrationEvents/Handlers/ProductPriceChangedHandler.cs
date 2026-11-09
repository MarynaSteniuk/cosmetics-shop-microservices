using Platform.EventBus.Events;
using Reviews.WebApi.IntegrationEvents.Events;

namespace Reviews.WebApi.IntegrationEvents.Handlers;

public sealed class ProductPriceChangedHandler : IIntegrationEventHandler<ProductPriceChangedEvent>
{
    private readonly ProductNameReplicator _replicator;

    public ProductPriceChangedHandler(ProductNameReplicator replicator)
    {
        _replicator = replicator;
    }

    public Task HandleAsync(ProductPriceChangedEvent @event, CancellationToken ct)
    {
        if (@event.ProductId <= 0 || string.IsNullOrWhiteSpace(@event.ProductName))
            throw new PermanentEventFailureException(
                $"Invalid {nameof(ProductPriceChangedEvent)} for product {@event.ProductId}: empty product name");

        return _replicator.ApplyAsync(@event.EventId, nameof(ProductPriceChangedEvent), @event.ProductId,
            @event.ProductName.Trim(), @event.OccurredAt, ct);
    }
}
