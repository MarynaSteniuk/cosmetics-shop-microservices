using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Catalog.Api.HealthChecks;

public static class CatalogHealthChecks
{
    public const string DatabaseCheckName = "catalog-db";
    public const string CacheCheckName = "redis-cache";

    public static IHealthChecksBuilder AddCatalogHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("CatalogDb")
                                  ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing"),
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
