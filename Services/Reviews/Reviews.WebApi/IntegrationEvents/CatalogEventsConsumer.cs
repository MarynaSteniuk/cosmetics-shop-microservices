using Platform.EventBus.Consuming;
using Platform.EventBus.Topology;
using RabbitMQ.Client;
using Reviews.WebApi.IntegrationEvents.Events;
using Reviews.WebApi.IntegrationEvents.Handlers;

namespace Reviews.WebApi.IntegrationEvents;

public sealed class CatalogEventsConsumer : RabbitMqConsumer
{
    public CatalogEventsConsumer(IConnection connection, IServiceScopeFactory scopeFactory,
        ILogger<CatalogEventsConsumer> logger)
        : base(connection, scopeFactory, logger)
    {
    }

    protected override ConsumerTopology Topology { get; } = new(
        EventBusNames.Exchanges.CatalogEvents,
        EventBusNames.Queues.ReviewsCatalogSync,
        [EventBusNames.RoutingKeys.AllProductEvents]);

    protected override void ConfigureHandlers(EventHandlerRegistry handlers)
        => handlers
            .Add<ProductRenamedEvent, ProductRenamedHandler>()
            .Add<ProductPriceChangedEvent, ProductPriceChangedHandler>();
}
