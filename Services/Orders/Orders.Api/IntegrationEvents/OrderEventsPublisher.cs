using Orders.Api.IntegrationEvents.Events;
using Orders.Bll.Events;
using Platform.EventBus.Publishing;
using Platform.EventBus.Topology;
using Platform.ServiceDefaults.Correlation;

namespace Orders.Api.IntegrationEvents;

public sealed class OrderEventsPublisher : IOrderEventsPublisher
{
    private readonly IEventPublisher _publisher;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<OrderEventsPublisher> _logger;

    public OrderEventsPublisher(IEventPublisher publisher, IHttpContextAccessor httpContext,
        ILogger<OrderEventsPublisher> logger)
    {
        _publisher = publisher;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task StockConfirmedAsync(long orderId, long customerId, IReadOnlyList<OrderStockLine> lines,
        DateTime confirmedAt, CancellationToken ct)
    {
        var @event = new OrderStockConfirmedEvent(orderId, customerId,
            lines.Select(l => new OrderStockConfirmedItem(l.ProductId, l.Units)).ToList())
        {
            OccurredAt = confirmedAt,
            CorrelationId = CurrentCorrelationId()
        };

        try
        {
            await _publisher.PublishAsync(@event, EventBusNames.Exchanges.OrdersEvents,
                EventBusNames.RoutingKeys.OrderStockConfirmed, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Order {OrderId} was confirmed, but {EventType} {EventId} was not published; catalog stock stays stale",
                orderId, nameof(OrderStockConfirmedEvent), @event.EventId);
        }
    }

    private string? CurrentCorrelationId()
    {
        var value = _httpContext.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
