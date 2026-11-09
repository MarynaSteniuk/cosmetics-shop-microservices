using Reviews.WebApi.IntegrationEvents.Handlers;

namespace Reviews.WebApi.IntegrationEvents;

public static class ReviewsIntegrationEventsExtensions
{
    public static IServiceCollection AddReviewsIntegrationEvents(this IServiceCollection services)
    {
        services.AddScoped<ProductNameReplicator>();
        services.AddScoped<ProductRenamedHandler>();
        services.AddScoped<ProductPriceChangedHandler>();
        services.AddIntegrationEventConsumer<CatalogEventsConsumer>();
        return services;
    }
}
