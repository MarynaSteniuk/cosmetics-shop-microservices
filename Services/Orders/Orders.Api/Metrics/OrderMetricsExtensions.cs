namespace Orders.Api.Metrics;

public static class OrderMetricsExtensions
{
    public static IServiceCollection AddOrderMetrics(this IServiceCollection services)
    {
        services.AddSingleton<OrderMetrics>();
        services.AddHostedService<OrdersMetricsSnapshotService>();
        return services;
    }
}
