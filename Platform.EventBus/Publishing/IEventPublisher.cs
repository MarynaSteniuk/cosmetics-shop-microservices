using Platform.EventBus.Events;

namespace Platform.EventBus.Publishing;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, string exchange, string routingKey, CancellationToken ct = default)
        where TEvent : IntegrationEvent;
}
