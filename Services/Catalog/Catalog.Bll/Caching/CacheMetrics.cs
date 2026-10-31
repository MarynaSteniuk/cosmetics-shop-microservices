using System.Diagnostics.Metrics;

namespace Catalog.Bll.Caching;

public sealed class CacheMetrics
{
    public const string MeterName = "Cosmetics.Cache";
    public const string MemoryLevel = "L1";
    public const string RedisLevel = "L2";

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _invalidations;
    private readonly Counter<long> _evictions;
    private readonly Histogram<double> _redisDuration;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _hits = meter.CreateCounter<long>("cache.hits", description: "Cache hits by level (L1 memory, L2 Redis)");
        _misses = meter.CreateCounter<long>("cache.misses", description: "Cache misses by level (L1 memory, L2 Redis)");
        _invalidations = meter.CreateCounter<long>("cache.invalidations", description: "Keys removed after writes");
        _evictions = meter.CreateCounter<long>("cache.evictions", description: "L1 entries evicted by expiration or size limit");
        _redisDuration = meter.CreateHistogram<double>("cache.redis.duration", unit: "ms",
            description: "Duration of Redis (L2) cache operations");
    }

    public void Hit(string level, string cacheName)
        => _hits.Add(1, new KeyValuePair<string, object?>("cache.level", level),
            new KeyValuePair<string, object?>("cache.name", cacheName));

    public void Miss(string level, string cacheName)
        => _misses.Add(1, new KeyValuePair<string, object?>("cache.level", level),
            new KeyValuePair<string, object?>("cache.name", cacheName));

    public void Invalidated(string reason)
        => _invalidations.Add(1, new KeyValuePair<string, object?>("cache.reason", reason));

    public void Evicted(string reason)
        => _evictions.Add(1, new KeyValuePair<string, object?>("cache.eviction_reason", reason));

    public void RedisOperation(string operation, double elapsedMs)
        => _redisDuration.Record(elapsedMs, new KeyValuePair<string, object?>("cache.operation", operation));
}
