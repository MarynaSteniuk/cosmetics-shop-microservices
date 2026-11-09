using Orders.Api.IntegrationEvents.Events;
using Orders.Api.IntegrationEvents.Handlers;
using Platform.EventBus.Consuming;
using Platform.EventBus.Topology;
using RabbitMQ.Client;

namespace Orders.Api.IntegrationEvents;

public sealed class CatalogEventsConsumer : RabbitMqConsumer
{
    public CatalogEventsConsumer(IConnection connection, IServiceScopeFactory scopeFactory,
        ILogger<CatalogEventsConsumer> logger)
        : base(connection, scopeFactory, logger)
    {
    }

    protected override ConsumerTopology Topology { get; } = new(
        EventBusNames.Exchanges.CatalogEvents,
        EventBusNames.Queues.OrdersCatalogSync,
        [EventBusNames.RoutingKeys.AllProductEvents]);

    protected override void ConfigureHandlers(EventHandlerRegistry handlers)
        => handlers
            .Add<ProductPriceChangedEvent, ProductPriceChangedHandler>()
            .Add<ProductRenamedEvent, ProductRenamedHandler>();
}
