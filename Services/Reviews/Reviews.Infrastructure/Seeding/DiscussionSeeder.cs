using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public sealed class DiscussionSeeder : IDataSeeder
{
    private readonly MongoDbContext _context;
    private readonly ILogger<DiscussionSeeder> _logger;

    public DiscussionSeeder(MongoDbContext context, ILogger<DiscussionSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await _context.Discussions.EstimatedDocumentCountAsync(cancellationToken: ct);
        if (existing > 0)
        {
            _logger.LogInformation("Collection {Collection} already has {Count} documents, seeding skipped",
                MongoDbContext.DiscussionsCollectionName, existing);
            return;
        }

        var cleanser = new Discussion(1, "CeraVe Hydrating Cleanser 236 ml",
            "Чи підходить для чутливої шкіри?", new Author(3, "Ірина С."), ["sensitive", "cleansing"]);
        cleanser.RegisterMessage(ObjectId.GenerateNewId(), "Олена К.", "У мене суха чутлива шкіра, подразнень не було.",
            DateTime.UtcNow.AddDays(-3));

        var spf = new Discussion(8, "Avène Cleanance SPF50+ 50 ml",
            "Як лягає під макіяж?", new Author(1, "Олена К."), ["spf", "makeup"]);

        var discussions = new[] { cleanser, spf };
        await _context.Discussions.InsertManyAsync(discussions, cancellationToken: ct);

        _logger.LogInformation("Seeded {Count} documents into {Collection}",
            discussions.Length, MongoDbContext.DiscussionsCollectionName);
    }
}
