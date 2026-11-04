using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace Platform.Aggregator.Resilience;

public static class GrpcRetryPolicies
{
    public static ServiceConfig For(ServiceResiliencePolicy policy) => new()
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = policy.MaxRetryAttempts + 1,
                    InitialBackoff = policy.RetryDelay,
                    MaxBackoff = policy.RetryDelay * 8,
                    BackoffMultiplier = 2,
                    RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted }
                }
            }
        }
    };

    public static DateTime DeadlineFor(ServiceResiliencePolicy policy) => DateTime.UtcNow.Add(policy.TotalRequestTimeout);
}
