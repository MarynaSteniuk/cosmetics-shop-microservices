using Microsoft.Extensions.DependencyInjection;
using Platform.EventBus.Events;

namespace Platform.EventBus.Consuming;

public sealed record EventHandlerRegistration(
    Type EventType,
    Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> Handle);

public sealed class EventHandlerRegistry
{
    private readonly Dictionary<string, EventHandlerRegistration> _registrations = new(StringComparer.Ordinal);

    public EventHandlerRegistry Add<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        _registrations[typeof(TEvent).Name] = new EventHandlerRegistration(
            typeof(TEvent),
            (services, @event, ct) => services.GetRequiredService<THandler>().HandleAsync((TEvent)@event, ct));
        return this;
    }

    public bool TryGet(string? eventType, out EventHandlerRegistration registration)
    {
        if (eventType is not null && _registrations.TryGetValue(eventType, out var found))
        {
            registration = found;
            return true;
        }

        registration = null!;
        return false;
    }
}
