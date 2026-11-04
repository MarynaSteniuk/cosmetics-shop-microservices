using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Reviews.Infrastructure.Persistence;

namespace Reviews.WebApi.HealthChecks;

public static class ReviewsHealthChecks
{
    public const string DatabaseCheckName = "reviews-db";
    public const string CacheCheckName = "redis-cache";

    public static IHealthChecksBuilder AddReviewsHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddHealthChecks()
            .AddMongoDb(
                sp => sp.GetRequiredService<MongoDbContext>().Client,
                sp => sp.GetRequiredService<IOptions<MongoDbOptions>>().Value.DatabaseName,
                name: DatabaseCheckName,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "mongodb", "ready"],
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
