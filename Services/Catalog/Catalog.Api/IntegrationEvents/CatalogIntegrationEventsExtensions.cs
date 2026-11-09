using Catalog.Api.IntegrationEvents.Handlers;
using Catalog.Bll.Events;

namespace Catalog.Api.IntegrationEvents;

public static class CatalogIntegrationEventsExtensions
{
    public static IServiceCollection AddCatalogIntegrationEvents(this IServiceCollection services)
    {
        services.AddScoped<IProductEventsPublisher, CatalogEventsPublisher>();
        services.AddScoped<OrderStockConfirmedHandler>();
        services.AddIntegrationEventConsumer<OrderEventsConsumer>();
        return services;
    }
}
