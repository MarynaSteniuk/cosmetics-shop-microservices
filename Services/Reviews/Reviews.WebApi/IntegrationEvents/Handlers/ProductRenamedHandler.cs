using Platform.EventBus.Events;
using Reviews.WebApi.IntegrationEvents.Events;

namespace Reviews.WebApi.IntegrationEvents.Handlers;

public sealed class ProductRenamedHandler : IIntegrationEventHandler<ProductRenamedEvent>
{
    private readonly ProductNameReplicator _replicator;

    public ProductRenamedHandler(ProductNameReplicator replicator)
    {
        _replicator = replicator;
    }

    public Task HandleAsync(ProductRenamedEvent @event, CancellationToken ct)
    {
        if (@event.ProductId <= 0 || string.IsNullOrWhiteSpace(@event.NewName))
            throw new PermanentEventFailureException(
                $"Invalid {nameof(ProductRenamedEvent)} for product {@event.ProductId}: empty name");

        return _replicator.ApplyAsync(@event.EventId, nameof(ProductRenamedEvent), @event.ProductId,
            @event.NewName.Trim(), @event.OccurredAt, ct);
    }
}
