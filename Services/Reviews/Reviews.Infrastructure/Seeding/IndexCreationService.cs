using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public sealed class IndexCreationService : IHostedService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<IndexCreationService> _logger;

    public IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var reviewKeys = Builders<Review>.IndexKeys;

        await CreateManyAsync(_context.Reviews,
        [
            new CreateIndexModel<Review>(
                reviewKeys.Ascending(r => r.ProductId).Descending(r => r.CreatedAt),
                new CreateIndexOptions { Name = "ix_reviews_product_createdAt" }),
            new CreateIndexModel<Review>(
                reviewKeys.Ascending("author.customerId"),
                new CreateIndexOptions { Name = "ix_reviews_author_customerId" }),
            new CreateIndexModel<Review>(
                reviewKeys.Text(r => r.Title).Text(r => r.Text),
                new CreateIndexOptions { Name = "ix_reviews_text", DefaultLanguage = "none" }),
            new CreateIndexModel<Review>(
                reviewKeys.Ascending(r => r.ProductId).Ascending("author.customerId"),
                new CreateIndexOptions<Review>
                {
                    Name = "ux_reviews_product_customer",
                    Unique = true,
                    PartialFilterExpression = new BsonDocument("author.customerId", new BsonDocument("$gt", 0))
                })
        ], cancellationToken);

        var discussionKeys = Builders<Discussion>.IndexKeys;

        await CreateManyAsync(_context.Discussions,
        [
            new CreateIndexModel<Discussion>(
                discussionKeys.Ascending(d => d.ProductId).Descending(d => d.LastMessageAt),
                new CreateIndexOptions { Name = "ix_discussions_product_lastMessageAt" }),
            new CreateIndexModel<Discussion>(
                discussionKeys.Ascending("tags"),
                new CreateIndexOptions { Name = "ix_discussions_tags" })
        ], cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateManyAsync<T>(IMongoCollection<T> collection, IReadOnlyList<CreateIndexModel<T>> models,
        CancellationToken ct)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;

        try
        {
            var names = await collection.Indexes.CreateManyAsync(models, ct);
            _logger.LogInformation("Indexes {IndexNames} are ready on {Collection}", names, collectionName);
        }
        catch (MongoCommandException ex) when (ex.Code is 85 or 86)
        {
            _logger.LogWarning("Some indexes on {Collection} already exist with other options: {Reason}",
                collectionName, ex.Message);
        }
    }
}
