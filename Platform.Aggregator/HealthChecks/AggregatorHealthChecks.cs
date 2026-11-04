using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.Aggregator.Resilience;

namespace Platform.Aggregator.HealthChecks;

public static class AggregatorHealthChecks
{
    public static IHealthChecksBuilder AddAggregatorHealthChecks(this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = services.AddHealthChecks();

        foreach (var policy in ResiliencePolicies.All)
        {
            var clientName = $"{policy.ServiceName}-health";

#pragma warning disable EXTEXP0001
            services.AddGrpcClient<Health.HealthClient>(clientName,
                    options => options.Address = new Uri($"https://{policy.ServiceName}"))
                .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

            builder.Add(new HealthCheckRegistration(
                policy.ServiceName,
                sp => new GrpcDependencyHealthCheck(
                    sp.GetRequiredService<GrpcClientFactory>().CreateClient<Health.HealthClient>(clientName),
                    policy.ServiceName,
                    sp.GetRequiredService<ILogger<GrpcDependencyHealthCheck>>()),
                policy.IsCritical ? HealthStatus.Unhealthy : HealthStatus.Degraded,
                ["grpc", "downstream", "ready"],
                TimeSpan.FromSeconds(3)));
        }

        var redis = configuration.GetConnectionString("redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            builder.AddRedis(
                redisConnectionString: redis,
                name: "redis-cache",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "ready"],
                timeout: TimeSpan.FromSeconds(3));
        }

        return builder;
    }
}
