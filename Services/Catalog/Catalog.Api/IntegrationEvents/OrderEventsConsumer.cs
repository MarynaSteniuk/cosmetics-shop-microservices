using Catalog.Api.IntegrationEvents.Events;
using Catalog.Api.IntegrationEvents.Handlers;
using Platform.EventBus.Consuming;
using Platform.EventBus.Topology;
using RabbitMQ.Client;

namespace Catalog.Api.IntegrationEvents;

public sealed class OrderEventsConsumer : RabbitMqConsumer
{
    public OrderEventsConsumer(IConnection connection, IServiceScopeFactory scopeFactory,
        ILogger<OrderEventsConsumer> logger)
        : base(connection, scopeFactory, logger)
    {
    }

    protected override ConsumerTopology Topology { get; } = new(
        EventBusNames.Exchanges.OrdersEvents,
        EventBusNames.Queues.CatalogOrdersSync,
        [EventBusNames.RoutingKeys.OrderStockConfirmed]);

    protected override void ConfigureHandlers(EventHandlerRegistry handlers)
        => handlers.Add<OrderStockConfirmedEvent, OrderStockConfirmedHandler>();
}
