using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public sealed class ReviewRepository : IReviewRepository
{
    private static readonly string[] MutableFields =
        ["rating", "title", "text", "comments", "photos", "usageDuration", "discussionId", "helpfulVotes", "updatedAt", "version"];

    private readonly IMongoCollection<Review> _reviews;

    public ReviewRepository(MongoDbContext context)
    {
        _reviews = context.Reviews;
    }

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _reviews.Find(r => r.Id == id).FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(long productId, long customerId, CancellationToken ct = default)
        => _reviews.Find(r => r.ProductId == productId && r.Author.CustomerId == customerId).AnyAsync(ct);

    public Task AddAsync(Review review, CancellationToken ct = default)
        => _reviews.InsertOneAsync(review, cancellationToken: ct);

    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        var filters = Builders<Review>.Filter;

        var versionFilter = expectedVersion == 0
            ? filters.Or(filters.Eq(r => r.Version, 0), filters.Exists(r => r.Version, false))
            : filters.Eq(r => r.Version, expectedVersion);

        review.BumpVersion();

        var document = review.ToBsonDocument();
        var set = new BsonDocument();
        var unset = new BsonDocument();

        foreach (var field in MutableFields)
        {
            if (document.TryGetValue(field, out var value))
                set[field] = value;
            else
                unset[field] = "";
        }

        var update = new BsonDocument("$set", set);
        if (unset.ElementCount > 0)
            update["$unset"] = unset;

        var result = await _reviews.UpdateOneAsync(
            filters.Eq(r => r.Id, review.Id) & versionFilter,
            update,
            cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConflictException(
                $"Review {review.Id} was modified by another request (expected version {expectedVersion})");
    }

    public async Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteOneAsync(r => r.Id == id, ct);
        return result.DeletedCount > 0;
    }
}
