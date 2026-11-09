using Orders.Api.IntegrationEvents.Events;
using Orders.Dal.Interfaces;
using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents.Handlers;

public sealed class ProductPriceChangedHandler : IIntegrationEventHandler<ProductPriceChangedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductPriceChangedHandler> _logger;

    public ProductPriceChangedHandler(IUnitOfWork uow, ILogger<ProductPriceChangedHandler> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task HandleAsync(ProductPriceChangedEvent @event, CancellationToken ct)
    {
        if (@event.ProductId <= 0 || @event.NewPrice <= 0 || string.IsNullOrWhiteSpace(@event.ProductName))
            throw new PermanentEventFailureException(
                $"Invalid {nameof(ProductPriceChangedEvent)} for product {@event.ProductId}: price {@event.NewPrice}");

        await _uow.BeginTransactionAsync(ct: ct);
        try
        {
            if (!await _uow.ProcessedEvents.TryAddAsync(@event.EventId, nameof(ProductPriceChangedEvent), ct))
            {
                await _uow.RollbackAsync(ct);
                _logger.LogInformation("Duplicate {EventType} {EventId} for product {ProductId} skipped",
                    nameof(ProductPriceChangedEvent), @event.EventId, @event.ProductId);
                return;
            }

            var result = await _uow.Products.ApplyCatalogChangeAsync(@event.ProductId, @event.ProductName,
                @event.NewPrice, @event.OccurredAt, ct);

            await _uow.CommitAsync(ct);

            _logger.LogInformation(
                "Local product {ProductId} {SyncResult} from {EventType} {EventId}: price {OldPrice} -> {NewPrice}, name {ProductName}; order item prices are not changed",
                @event.ProductId, result, nameof(ProductPriceChangedEvent), @event.EventId, @event.OldPrice,
                @event.NewPrice, @event.ProductName);
        }
        catch (Exception ex)
        {
            await _uow.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to apply {EventType} {EventId} to local product {ProductId}",
                nameof(ProductPriceChangedEvent), @event.EventId, @event.ProductId);
            throw;
        }
    }
}
