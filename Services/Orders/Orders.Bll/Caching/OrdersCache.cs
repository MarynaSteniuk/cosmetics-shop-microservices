namespace Orders.Bll.Caching;

public static class OrdersCacheKeys
{
    public static string Order(long id) => $"orders:order:{id}";

    public static string Customer(long id) => $"orders:customer:{id}";

    public static string AggregatedOrder(long id) => $"aggregator:order:{id}:full";
}

public static class OrdersCacheProfiles
{
    public static readonly CacheProfile Order = new("orders.order",
        memoryTtl: TimeSpan.FromSeconds(30), distributedTtl: TimeSpan.FromMinutes(2),
        memorySliding: TimeSpan.FromSeconds(15));

    public static readonly CacheProfile Customer = new("orders.customer",
        memoryTtl: TimeSpan.FromSeconds(20), distributedTtl: TimeSpan.FromMinutes(1),
        memorySliding: TimeSpan.FromSeconds(10));
}
