using Orders.Api.IntegrationEvents.Events;
using Orders.Dal.Interfaces;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents.Handlers;

public sealed class ProductRenamedHandler : IIntegrationEventHandler<ProductRenamedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductRenamedHandler> _logger;

    public ProductRenamedHandler(IUnitOfWork uow, ILogger<ProductRenamedHandler> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(ProductRenamedEvent @event, CancellationToken ct)
    {
        if (@event.ProductId <= 0 || string.IsNullOrWhiteSpace(@event.NewName))
            throw new PermanentEventFailureException(
                $"Invalid {nameof(ProductRenamedEvent)} for product {@event.ProductId}: empty name");

        await _uow.BeginTransactionAsync(ct: ct);
        try
        {
            if (!await _uow.ProcessedEvents.TryAddAsync(@event.EventId, nameof(ProductRenamedEvent), ct))
            {
                await _uow.RollbackAsync(ct);
                _logger.LogInformation("Duplicate {EventType} {EventId} for product {ProductId} skipped",
                    nameof(ProductRenamedEvent), @event.EventId, @event.ProductId);
                return;
            }

            var result = await _uow.Products.ApplyCatalogChangeAsync(@event.ProductId, @event.NewName, null,
                @event.OccurredAt, ct);

            await _uow.CommitAsync(ct);

            _logger.LogInformation(
                "Local product {ProductId} {SyncResult} from {EventType} {EventId}: name {OldName} -> {NewName}; names in placed orders are not changed",
                @event.ProductId, result, nameof(ProductRenamedEvent), @event.EventId, @event.OldName, @event.NewName);
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to apply {EventType} {EventId} to local product {ProductId}",
                nameof(ProductRenamedEvent), @event.EventId, @event.ProductId);
            throw;
        }
    }
}
