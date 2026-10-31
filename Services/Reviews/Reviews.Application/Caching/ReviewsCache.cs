namespace Reviews.Application.Caching;

public static class ReviewsCacheKeys
{
    public static string Review(string id) => $"reviews:review:{id}";

    public static string ProductSummary(long productId) => $"reviews:product:{productId}:summary";
}

public static class ReviewsCacheProfiles
{
    public const int SummaryLatestCount = 10;

    public static readonly CacheProfile Review = new("reviews.review",
        memoryTtl: TimeSpan.FromMinutes(1), distributedTtl: TimeSpan.FromMinutes(5),
        memorySliding: TimeSpan.FromSeconds(30));

    public static readonly CacheProfile ProductSummary = new("reviews.product-summary",
        memoryTtl: TimeSpan.FromMinutes(1), distributedTtl: TimeSpan.FromMinutes(5));
}
