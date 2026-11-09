using Orders.Api.IntegrationEvents.Handlers;
using Orders.Bll.Events;

namespace Orders.Api.IntegrationEvents;

public static class OrdersIntegrationEventsExtensions
{
    public static IServiceCollection AddOrdersIntegrationEvents(this IServiceCollection services)
    {
        services.AddScoped<IOrderEventsPublisher, OrderEventsPublisher>();
        services.AddScoped<ProductPriceChangedHandler>();
        services.AddScoped<ProductRenamedHandler>();
        services.AddIntegrationEventConsumer<CatalogEventsConsumer>();
        return services;
    }
}
