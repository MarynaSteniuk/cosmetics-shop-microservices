using Platform.EventBus.Events;

namespace Reviews.WebApi.IntegrationEvents.Events;

public sealed record ProductRenamedEvent(
    long ProductId,
    string Sku,
    string OldName,
    string NewName) : IntegrationEvent;
