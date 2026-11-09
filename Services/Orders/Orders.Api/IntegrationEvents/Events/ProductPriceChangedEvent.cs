using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents.Events;

public sealed record ProductPriceChangedEvent(
    long ProductId,
    string Sku,
    string ProductName,
    decimal OldPrice,
    decimal NewPrice) : IntegrationEvent;
