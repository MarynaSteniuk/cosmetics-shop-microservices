using Microsoft.Extensions.Caching.Memory;

namespace Reviews.Application.Caching;

public sealed class CacheProfile
{
    public CacheProfile(string name, TimeSpan memoryTtl, TimeSpan distributedTtl, TimeSpan? memorySliding = null,
        CacheItemPriority priority = CacheItemPriority.Normal)
    {
        if (memoryTtl >= distributedTtl)
            throw new ArgumentException($"Cache profile {name}: L1 (memory) TTL must be shorter than L2 (Redis) TTL");

        if (memorySliding is { } sliding && sliding >= memoryTtl)
            throw new ArgumentException($"Cache profile {name}: sliding expiration must be shorter than absolute TTL");

        Name = name;
        MemoryTtl = memoryTtl;
        DistributedTtl = distributedTtl;
        MemorySliding = memorySliding;
        Priority = priority;
    }

    public string Name { get; }
    public TimeSpan MemoryTtl { get; }
    public TimeSpan DistributedTtl { get; }
    public TimeSpan? MemorySliding { get; }
    public CacheItemPriority Priority { get; }
}
