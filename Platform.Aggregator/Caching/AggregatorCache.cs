namespace Platform.Aggregator.Caching;

public static class AggregatorCacheKeys
{
    public static string FullOrder(long orderId) => $"aggregator:order:{orderId}:full";
}

public static class AggregatorCacheSettings
{
    public static readonly TimeSpan FullOrderTtl = TimeSpan.FromSeconds(30);
}
