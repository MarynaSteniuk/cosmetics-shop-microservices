using Platform.EventBus.Events;

namespace Catalog.Api.IntegrationEvents.Events;

public sealed record OrderStockConfirmedEvent(
    long OrderId,
    long CustomerId,
    IReadOnlyList<OrderStockConfirmedItem> Items) : IntegrationEvent;

public sealed record OrderStockConfirmedItem(long ProductId, int Units);
