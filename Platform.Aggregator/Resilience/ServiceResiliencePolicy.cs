namespace Platform.Aggregator.Resilience;

public enum ClientTransport
{
    Http,
    Grpc
}

public sealed record ServiceResiliencePolicy(
    string ServiceName,
    bool IsCritical,
    int MaxRetryAttempts,
    TimeSpan RetryDelay,
    TimeSpan AttemptTimeout,
    TimeSpan TotalRequestTimeout,
    double FailureRatio,
    int MinimumThroughput,
    TimeSpan SamplingDuration,
    TimeSpan BreakDuration)
{
    public TimeSpan WorstCaseDuration
    {
        get
        {
            var attempts = MaxRetryAttempts + 1;
            var delays = RetryDelay * (Math.Pow(2, MaxRetryAttempts) - 1);
            return AttemptTimeout * attempts + delays;
        }
    }

    public void EnsureConsistent()
    {
        if (TotalRequestTimeout < WorstCaseDuration)
            throw new InvalidOperationException(
                $"{ServiceName}: total timeout {TotalRequestTimeout} is shorter than the worst case of retries {WorstCaseDuration}");

        if (SamplingDuration < AttemptTimeout * 2)
            throw new InvalidOperationException(
                $"{ServiceName}: circuit breaker sampling duration must be at least twice the attempt timeout");
    }
}
