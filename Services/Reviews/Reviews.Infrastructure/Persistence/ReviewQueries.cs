using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Infrastructure.Persistence;

public sealed class ReviewQueries : IReviewQueries
{
    private readonly IMongoCollection<BsonDocument> _reviews;

    public ReviewQueries(MongoDbContext context)
    {
        _reviews = context.Database.GetCollection<BsonDocument>(MongoDbContext.ReviewsCollectionName);
    }

    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        return await _reviews.Aggregate()
            .Match(new BsonDocument("_id", objectId))
            .Project<ReviewDto>(BuildProjection(includeComments: true))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<ReviewDto>> GetListAsync(ReviewListFilter filter, CancellationToken ct = default)
    {
        var match = new BsonDocument();

        if (filter.ProductId is { } productId)
            match["productId"] = productId;
        if (filter.MinRating is { } minRating)
            match["rating"] = new BsonDocument("$gte", minRating);
        if (filter.Search is { } search)
            match["$text"] = new BsonDocument("$search", search);

        var totalCount = await _reviews.CountDocumentsAsync(match, cancellationToken: ct);

        var items = await _reviews.Aggregate()
            .Match(match)
            .Sort(new BsonDocument { { "createdAt", -1 }, { "_id", -1 } })
            .Skip((filter.Page - 1) * filter.PageSize)
            .Limit(filter.PageSize)
            .Project<ReviewDto>(BuildProjection(includeComments: false))
            .ToListAsync(ct);

        return new PagedResult<ReviewDto>(items, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<ReviewsSummaryDto> GetSummaryAsync(long productId, int latestCount, CancellationToken ct = default)
    {
        var match = new BsonDocument("productId", productId);

        var stats = await _reviews.Aggregate()
            .Match(match)
            .Group(new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "count", new BsonDocument("$sum", 1) },
                { "average", new BsonDocument("$avg", "$rating") }
            })
            .FirstOrDefaultAsync(ct);

        var latest = await _reviews.Aggregate()
            .Match(match)
            .Sort(new BsonDocument { { "createdAt", -1 }, { "_id", -1 } })
            .Limit(latestCount)
            .Project<ReviewDto>(BuildProjection(includeComments: false))
            .ToListAsync(ct);

        return new ReviewsSummaryDto
        {
            ProductId = productId,
            TotalCount = stats?["count"].ToInt64() ?? 0,
            AverageRating = stats is null || stats["average"].IsBsonNull
                ? null
                : Math.Round(stats["average"].ToDouble(), 2),
            Latest = latest
        };
    }

    private static BsonDocument BuildProjection(bool includeComments)
    {
        var projection = new BsonDocument
        {
            { "productId", 1 },
            { "productName", 1 },
            { "authorCustomerId", "$author.customerId" },
            { "authorName", "$author.displayName" },
            { "skinType", "$author.skinType" },
            { "rating", 1 },
            { "title", 1 },
            { "text", 1 },
            {
                "usageDuration", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$ifNull", new BsonArray { "$usageDuration", false }),
                    new BsonDocument("$concat", new BsonArray
                    {
                        new BsonDocument("$toString", "$usageDuration.value"), " ", "$usageDuration.unit"
                    }),
                    "$$REMOVE"
                })
            },
            { "photos", new BsonDocument("$ifNull", new BsonArray { "$photos", new BsonArray() }) },
            { "commentsCount", new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray { "$comments", new BsonArray() })) },
            { "discussionId", 1 },
            { "helpfulVotes", new BsonDocument("$ifNull", new BsonArray { "$helpfulVotes", 0 }) },
            { "createdAt", 1 },
            { "updatedAt", 1 },
            { "version", new BsonDocument("$ifNull", new BsonArray { "$version", 0 }) }
        };

        if (includeComments)
            projection["comments"] = new BsonDocument("$ifNull", new BsonArray { "$comments", new BsonArray() });

        return projection;
    }
}
