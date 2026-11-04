using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public sealed class GrpcDependencyHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(2);

    private readonly Health.HealthClient _client;
    private readonly string _serviceName;
    private readonly ILogger<GrpcDependencyHealthCheck> _logger;

    public GrpcDependencyHealthCheck(Health.HealthClient client, string serviceName,
        ILogger<GrpcDependencyHealthCheck> logger)
    {
        _client = client;
        _serviceName = serviceName;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reply = await _client.CheckAsync(new HealthCheckRequest(),
                deadline: DateTime.UtcNow.Add(Deadline), cancellationToken: cancellationToken);

            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy($"{_serviceName} is serving")
                : new HealthCheckResult(context.Registration.FailureStatus, $"{_serviceName} status: {reply.Status}");
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC health check of {Service} failed with {GrpcStatus}", _serviceName, ex.StatusCode);
            return new HealthCheckResult(context.Registration.FailureStatus,
                $"{_serviceName} is unavailable ({ex.StatusCode})", ex);
        }
    }
}
