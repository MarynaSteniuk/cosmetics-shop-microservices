namespace Catalog.Bll.Caching;

public interface ITwoLevelCache
{
    Task<T?> GetOrCreateAsync<T>(string key, CacheProfile profile, Func<CancellationToken, Task<T?>> factory,
        CancellationToken ct = default) where T : class;

    Task InvalidateAsync(IEnumerable<string> keys, string reason, CancellationToken ct = default);
}
