using Catalog.Api.IntegrationEvents.Events;
using Catalog.Bll.Events;
using Platform.EventBus.Events;
using Platform.EventBus.Publishing;
using Platform.EventBus.Topology;
using Platform.ServiceDefaults.Correlation;

namespace Catalog.Api.IntegrationEvents;

public sealed class CatalogEventsPublisher : IProductEventsPublisher
{
    private readonly IEventPublisher _publisher;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<CatalogEventsPublisher> _logger;

    public CatalogEventsPublisher(IEventPublisher publisher, IHttpContextAccessor httpContext,
        ILogger<CatalogEventsPublisher> logger)
    {
        _publisher = publisher;
        _httpContext = httpContext;
        _logger = logger;
    }

    public Task PriceChangedAsync(long productId, string sku, string productName, decimal oldPrice, decimal newPrice,
        DateTime changedAt, CancellationToken ct)
        => PublishAsync(new ProductPriceChangedEvent(productId, sku, productName, oldPrice, newPrice)
            {
                OccurredAt = changedAt,
                CorrelationId = CurrentCorrelationId()
            },
            EventBusNames.RoutingKeys.ProductPriceChanged, productId, ct);

    public Task RenamedAsync(long productId, string sku, string oldName, string newName, DateTime changedAt,
        CancellationToken ct)
        => PublishAsync(new ProductRenamedEvent(productId, sku, oldName, newName)
            {
                OccurredAt = changedAt,
                CorrelationId = CurrentCorrelationId()
            },
            EventBusNames.RoutingKeys.ProductRenamed, productId, ct);

    private async Task PublishAsync<TEvent>(TEvent @event, string routingKey, long productId, CancellationToken ct)
        where TEvent : IntegrationEvent
    {
        try
        {
            await _publisher.PublishAsync(@event, EventBusNames.Exchanges.CatalogEvents, routingKey, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Product {ProductId} was saved, but {EventType} {EventId} was not published; copies in other services stay stale until the next change",
                productId, typeof(TEvent).Name, @event.EventId);
        }
    }

    private string? CurrentCorrelationId()
    {
        var value = _httpContext.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
