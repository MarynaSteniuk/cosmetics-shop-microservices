using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

public static class CatalogCacheKeys
{
    public const string Categories = "catalog:categories:all";

    public static string Product(long id) => $"catalog:product:{id}";

    public static string CategoryProducts(int categoryId) => $"catalog:category:{categoryId}:products";
}

public static class CatalogCacheProfiles
{
    public static readonly CacheProfile Product = new("catalog.product",
        memoryTtl: TimeSpan.FromMinutes(2), distributedTtl: TimeSpan.FromMinutes(10),
        memorySliding: TimeSpan.FromMinutes(1));

    public static readonly CacheProfile Categories = new("catalog.categories",
        memoryTtl: TimeSpan.FromMinutes(10), distributedTtl: TimeSpan.FromHours(1),
        priority: CacheItemPriority.High);

    public static readonly CacheProfile CategoryProducts = new("catalog.category-products",
        memoryTtl: TimeSpan.FromMinutes(1), distributedTtl: TimeSpan.FromMinutes(5),
        memorySliding: TimeSpan.FromSeconds(30));
}
