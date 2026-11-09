using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Caching;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.WebApi.IntegrationEvents;

public sealed class ProductNameReplicator
{
    public const string ProcessedEventsCollection = "processed_events";
    public const string ProductNameSyncCollection = "product_name_sync";

    private readonly MongoDbContext _context;
    private readonly ITwoLevelCache _cache;
    private readonly ILogger<ProductNameReplicator> _logger;

    public ProductNameReplicator(MongoDbContext context, ITwoLevelCache cache, ILogger<ProductNameReplicator> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    private IMongoCollection<BsonDocument> ProcessedEvents
        => _context.Database.GetCollection<BsonDocument>(ProcessedEventsCollection);

    private IMongoCollection<BsonDocument> ProductNameSync
        => _context.Database.GetCollection<BsonDocument>(ProductNameSyncCollection);

    public async Task ApplyAsync(Guid eventId, string eventType, long productId, string productName,
        DateTime changedAt, CancellationToken ct)
    {
        changedAt = changedAt.Kind == DateTimeKind.Utc ? changedAt : DateTime.SpecifyKind(changedAt, DateTimeKind.Utc);

        if (!await TryMarkProcessedAsync(eventId, eventType, ct))
        {
            _logger.LogInformation("Duplicate {EventType} {EventId} for product {ProductId} skipped",
                eventType, eventId, productId);
            return;
        }

        try
        {
            if (await IsStaleAsync(productId, changedAt, ct))
            {
                _logger.LogInformation(
                    "Stale {EventType} {EventId} for product {ProductId} skipped: a newer name is already applied",
                    eventType, eventId, productId);
                return;
            }

            var reviewIds = await _context.Reviews
                .Find(r => r.ProductId == productId && r.ProductName != productName)
                .Project(r => r.Id)
                .ToListAsync(ct);

            var reviews = await _context.Reviews.UpdateManyAsync(
                r => r.ProductId == productId && r.ProductName != productName,
                Builders<Review>.Update.Set(r => r.ProductName, productName).Inc(r => r.Version, 1),
                cancellationToken: ct);

            var discussions = await _context.Discussions.UpdateManyAsync(
                d => d.ProductId == productId && d.ProductName != productName,
                Builders<Discussion>.Update.Set(d => d.ProductName, productName).Inc(d => d.Version, 1),
                cancellationToken: ct);

            await ProductNameSync.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", productId),
                Builders<BsonDocument>.Update.Max("syncedAt", changedAt).Set("productName", productName),
                new UpdateOptions { IsUpsert = true },
                ct);

            await _cache.InvalidateAsync(
                reviewIds.Select(id => ReviewsCacheKeys.Review(id.ToString()))
                    .Append(ReviewsCacheKeys.ProductSummary(productId)),
                eventType, ct);

            _logger.LogInformation(
                "Product {ProductId} name {ProductName} applied from {EventType} {EventId}: {ReviewsUpdated} reviews, {DiscussionsUpdated} discussions updated",
                productId, productName, eventType, eventId, reviews.ModifiedCount, discussions.ModifiedCount);
        }
        catch (Exception ex)
        {
            await ProcessedEvents.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", eventId.ToString()),
                CancellationToken.None);
            _logger.LogError(ex, "Failed to apply {EventType} {EventId} to reviews of product {ProductId}",
                eventType, eventId, productId);
            throw;
        }
    }

    private async Task<bool> TryMarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    {
        try
        {
            await ProcessedEvents.InsertOneAsync(new BsonDocument
            {
                { "_id", eventId.ToString() },
                { "eventType", eventType },
                { "processedAt", DateTime.UtcNow }
            }, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    private async Task<bool> IsStaleAsync(long productId, DateTime changedAt, CancellationToken ct)
    {
        var sync = await ProductNameSync.Find(Builders<BsonDocument>.Filter.Eq("_id", productId))
            .FirstOrDefaultAsync(ct);
        return sync is not null && sync["syncedAt"].ToUniversalTime() >= changedAt;
    }
}
