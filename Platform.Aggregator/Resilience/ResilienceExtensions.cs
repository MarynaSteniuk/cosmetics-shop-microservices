using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Platform.Aggregator.Resilience;

public static class ResilienceExtensions
{
    private const string LoggerCategory = "Platform.Aggregator.Resilience";

    public static IHttpClientBuilder AddServiceResilience(this IHttpClientBuilder builder,
        ServiceResiliencePolicy policy, ClientTransport transport)
    {
        policy.EnsureConsistent();

#pragma warning disable EXTEXP0001
        builder.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

        builder.AddStandardResilienceHandler().Configure((options, services) =>
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory);
            var service = policy.ServiceName;

            options.Retry.MaxRetryAttempts = policy.MaxRetryAttempts;
            options.Retry.Delay = policy.RetryDelay;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            if (transport == ClientTransport.Http)
                options.Retry.DisableForUnsafeHttpMethods();
            else
                options.Retry.ShouldHandle = static _ => ValueTask.FromResult(false);

            options.Retry.OnRetry = args =>
            {
                logger.LogWarning(
                    "Retry {AttemptNumber} for {Service} ({Transport}) in {DelayMs} ms, reason {Reason}",
                    args.AttemptNumber + 1, service, transport, args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Exception?.GetType().Name ?? args.Outcome.Result?.StatusCode.ToString());
                return ValueTask.CompletedTask;
            };

            options.AttemptTimeout.Timeout = policy.AttemptTimeout;
            options.TotalRequestTimeout.Timeout = policy.TotalRequestTimeout;

            options.CircuitBreaker.FailureRatio = policy.FailureRatio;
            options.CircuitBreaker.MinimumThroughput = policy.MinimumThroughput;
            options.CircuitBreaker.SamplingDuration = policy.SamplingDuration;
            options.CircuitBreaker.BreakDuration = policy.BreakDuration;

            options.CircuitBreaker.OnOpened = args =>
            {
                logger.LogError("Circuit opened for {Service} ({Transport}) for {BreakDurationSeconds} s, reason {Reason}",
                    service, transport, args.BreakDuration.TotalSeconds,
                    args.Outcome.Exception?.GetType().Name ?? args.Outcome.Result?.StatusCode.ToString());
                return ValueTask.CompletedTask;
            };
            options.CircuitBreaker.OnHalfOpened = _ =>
            {
                logger.LogWarning("Circuit half-opened for {Service} ({Transport}), sending a trial request",
                    service, transport);
                return ValueTask.CompletedTask;
            };
            options.CircuitBreaker.OnClosed = _ =>
            {
                logger.LogInformation("Circuit closed for {Service} ({Transport})", service, transport);
                return ValueTask.CompletedTask;
            };
        });

        return builder;
    }
}
