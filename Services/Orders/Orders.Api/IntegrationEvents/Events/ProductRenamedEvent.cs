using Platform.EventBus.Events;

namespace Orders.Api.IntegrationEvents.Events;

public sealed record ProductRenamedEvent(
    long ProductId,
    string Sku,
    string OldName,
    string NewName) : IntegrationEvent;
