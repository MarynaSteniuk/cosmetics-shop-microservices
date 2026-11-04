namespace Platform.Aggregator.Resilience;

public static class ResiliencePolicies
{
    public static readonly ServiceResiliencePolicy Orders = new(
        ServiceName: "orders-api",
        IsCritical: true,
        MaxRetryAttempts: 3,
        RetryDelay: TimeSpan.FromMilliseconds(500),
        AttemptTimeout: TimeSpan.FromSeconds(3),
        TotalRequestTimeout: TimeSpan.FromSeconds(16),
        FailureRatio: 0.3,
        MinimumThroughput: 10,
        SamplingDuration: TimeSpan.FromSeconds(30),
        BreakDuration: TimeSpan.FromSeconds(15));

    public static readonly ServiceResiliencePolicy Catalog = new(
        ServiceName: "catalog-api",
        IsCritical: true,
        MaxRetryAttempts: 4,
        RetryDelay: TimeSpan.FromMilliseconds(200),
        AttemptTimeout: TimeSpan.FromSeconds(2),
        TotalRequestTimeout: TimeSpan.FromSeconds(15),
        FailureRatio: 0.3,
        MinimumThroughput: 10,
        SamplingDuration: TimeSpan.FromSeconds(30),
        BreakDuration: TimeSpan.FromSeconds(15));

    public static readonly ServiceResiliencePolicy Reviews = new(
        ServiceName: "reviews-api",
        IsCritical: false,
        MaxRetryAttempts: 1,
        RetryDelay: TimeSpan.FromMilliseconds(200),
        AttemptTimeout: TimeSpan.FromSeconds(2),
        TotalRequestTimeout: TimeSpan.FromSeconds(5),
        FailureRatio: 0.7,
        MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(30),
        BreakDuration: TimeSpan.FromSeconds(30));

    public static IReadOnlyList<ServiceResiliencePolicy> All { get; } = [Orders, Catalog, Reviews];
}
