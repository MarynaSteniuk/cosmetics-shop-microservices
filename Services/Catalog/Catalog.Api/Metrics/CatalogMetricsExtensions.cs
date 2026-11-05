namespace Catalog.Api.Metrics;

public static class CatalogMetricsExtensions
{
    public static IServiceCollection AddCatalogMetrics(this IServiceCollection services)
    {
        services.AddSingleton<CatalogMetrics>();
        services.AddHostedService<CatalogMetricsSnapshotService>();
        return services;
    }
}
