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

        await CreateAsync(_context.Reviews, new CreateIndexModel<Review>(
            reviewKeys.Ascending(r => r.ProductId).Descending(r => r.CreatedAt),
            new CreateIndexOptions { Name = "ix_reviews_product_createdAt" }), cancellationToken);

        await CreateAsync(_context.Reviews, new CreateIndexModel<Review>(
            reviewKeys.Ascending("author.customerId"),
            new CreateIndexOptions { Name = "ix_reviews_author_customerId" }), cancellationToken);

        await CreateAsync(_context.Reviews, new CreateIndexModel<Review>(
            reviewKeys.Text(r => r.Title).Text(r => r.Text),
            new CreateIndexOptions { Name = "ix_reviews_text", DefaultLanguage = "none" }), cancellationToken);

        await CreateAsync(_context.Reviews, new CreateIndexModel<Review>(
            reviewKeys.Ascending(r => r.ProductId).Ascending("author.customerId"),
            new CreateIndexOptions<Review>
            {
                Name = "ux_reviews_product_customer",
                Unique = true,
                PartialFilterExpression = new BsonDocument("author.customerId", new BsonDocument("$gt", 0))
            }), cancellationToken);

        var discussionKeys = Builders<Discussion>.IndexKeys;

        await CreateAsync(_context.Discussions, new CreateIndexModel<Discussion>(
            discussionKeys.Ascending(d => d.ProductId).Descending(d => d.LastMessageAt),
            new CreateIndexOptions { Name = "ix_discussions_product_lastMessageAt" }), cancellationToken);

        await CreateAsync(_context.Discussions, new CreateIndexModel<Discussion>(
            discussionKeys.Ascending("tags"),
            new CreateIndexOptions { Name = "ix_discussions_tags" }), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task CreateAsync<T>(IMongoCollection<T> collection, CreateIndexModel<T> model, CancellationToken ct)
    {
        try
        {
            var name = await collection.Indexes.CreateOneAsync(model, cancellationToken: ct);
            _logger.LogInformation("Index {IndexName} is ready on {Collection}",
                name, collection.CollectionNamespace.CollectionName);
        }
        catch (MongoCommandException ex) when (ex.Code is 85 or 86)
        {
            _logger.LogWarning("Index {IndexName} on {Collection} already exists with other options: {Reason}",
                model.Options?.Name, collection.CollectionNamespace.CollectionName, ex.Message);
        }
    }
}
