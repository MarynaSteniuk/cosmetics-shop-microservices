using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long SlowRequestThresholdMs = 500;

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
            _logger.LogWarning("Slow request {RequestName}: {ElapsedMs} ms (threshold {ThresholdMs} ms)",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds, SlowRequestThresholdMs);
        else
            _logger.LogInformation("{RequestName} completed in {ElapsedMs} ms",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
