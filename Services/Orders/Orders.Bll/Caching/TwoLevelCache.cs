using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Orders.Bll.Caching;

public sealed class TwoLevelCache : ITwoLevelCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IMemoryCache _memory;
    private readonly IDistributedCache _distributed;
    private readonly CacheMetrics _metrics;
    private readonly ILogger<TwoLevelCache> _logger;

    public TwoLevelCache(IMemoryCache memory, IDistributedCache distributed, CacheMetrics metrics,
        ILogger<TwoLevelCache> logger)
    {
        _memory = memory;
        _distributed = distributed;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, CacheProfile profile, Func<CancellationToken, Task<T?>> loadFromDatabase,
        CancellationToken ct = default) where T : class
    {
        if (_memory.TryGetValue(key, out T? fromMemory) && fromMemory is not null)
        {
            _metrics.Hit(CacheMetrics.MemoryLevel, profile.Name);
            _logger.LogDebug("Cache hit {CacheLevel} {CacheKey}", CacheMetrics.MemoryLevel, key);
            return fromMemory;
        }

        _metrics.Miss(CacheMetrics.MemoryLevel, profile.Name);
        _logger.LogDebug("Cache miss {CacheLevel} {CacheKey}", CacheMetrics.MemoryLevel, key);

        var fromRedis = await ReadDistributedAsync<T>(key, profile, ct);
        if (fromRedis is not null)
        {
            SetMemory(key, fromRedis, profile);
            return fromRedis;
        }

        var value = await loadFromDatabase(ct);
        if (value is null)
            return null;

        _logger.LogDebug("Loaded {CacheKey} from database, filling L2 and L1", key);

        await WriteDistributedAsync(key, value, profile, ct);
        SetMemory(key, value, profile);
        return value;
    }

    public async Task InvalidateAsync(IEnumerable<string> keys, string reason, CancellationToken ct = default)
    {
        foreach (var key in keys.Distinct())
        {
            _memory.Remove(key);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _distributed.RemoveAsync(key, ct);
                _metrics.RedisOperation("remove", stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Redis is unavailable, {CacheKey} was removed only from memory", key);
            }

            _metrics.Invalidated(reason);
            _logger.LogInformation("Cache invalidated {CacheKey}, reason {Reason}", key, reason);
        }
    }

    private async Task<T?> ReadDistributedAsync<T>(string key, CacheProfile profile, CancellationToken ct) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var bytes = await _distributed.GetAsync(key, ct);
            _metrics.RedisOperation("get", stopwatch.Elapsed.TotalMilliseconds);

            if (bytes is null)
            {
                _metrics.Miss(CacheMetrics.RedisLevel, profile.Name);
                _logger.LogDebug("Cache miss {CacheLevel} {CacheKey}, loading from database", CacheMetrics.RedisLevel, key);
                return null;
            }

            _metrics.Hit(CacheMetrics.RedisLevel, profile.Name);
            _logger.LogDebug("Cache hit {CacheLevel} {CacheKey}, {SizeBytes} bytes", CacheMetrics.RedisLevel, key, bytes.Length);
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis is unavailable, {CacheKey} is read from the database", key);
            return null;
        }
    }

    private async Task WriteDistributedAsync<T>(string key, T value, CacheProfile profile, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await _distributed.SetAsync(key, bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = profile.DistributedTtl }, ct);
            _metrics.RedisOperation("set", stopwatch.Elapsed.TotalMilliseconds);

            _logger.LogDebug("Cache set {CacheLevel} {CacheKey}, {SizeBytes} bytes, TTL {TtlSeconds} s",
                CacheMetrics.RedisLevel, key, bytes.Length, profile.DistributedTtl.TotalSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis is unavailable, {CacheKey} is cached only in memory", key);
        }
    }

    private void SetMemory<T>(string key, T value, CacheProfile profile)
    {
        var options = new MemoryCacheEntryOptions
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = profile.MemoryTtl,
            SlidingExpiration = profile.MemorySliding,
            Priority = profile.Priority
        };
        options.RegisterPostEvictionCallback(OnMemoryEviction);

        _memory.Set(key, value, options);
        _logger.LogDebug("Cache set {CacheLevel} {CacheKey}, TTL {TtlSeconds} s",
            CacheMetrics.MemoryLevel, key, profile.MemoryTtl.TotalSeconds);
    }

    private void OnMemoryEviction(object key, object? value, EvictionReason reason, object? state)
    {
        if (reason is EvictionReason.Removed or EvictionReason.Replaced)
            return;

        _metrics.Evicted(reason.ToString());
        _logger.LogDebug("Cache evicted {CacheLevel} {CacheKey}, reason {EvictionReason}",
            CacheMetrics.MemoryLevel, key, reason);
    }
}
