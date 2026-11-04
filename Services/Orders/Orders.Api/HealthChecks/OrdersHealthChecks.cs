using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orders.Api.HealthChecks;

public static class OrdersHealthChecks
{
    public const string DatabaseCheckName = "orders-db";
    public const string CacheCheckName = "redis-cache";

    public static IHealthChecksBuilder AddOrdersHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("OrdersDb")
                                  ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing"),
                healthQuery: "SELECT 1;",
                name: DatabaseCheckName,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "sql", "ready"],
                timeout: TimeSpan.FromSeconds(5));

        var redis = configuration.GetConnectionString("redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            builder.AddRedis(
                redisConnectionString: redis,
                name: CacheCheckName,
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "ready"],
                timeout: TimeSpan.FromSeconds(3));
        }

        return builder;
    }
}
