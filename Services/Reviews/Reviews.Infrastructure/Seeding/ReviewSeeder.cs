using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

public sealed class ReviewSeeder : IDataSeeder
{
    private readonly MongoDbContext _context;
    private readonly ILogger<ReviewSeeder> _logger;

    public ReviewSeeder(MongoDbContext context, ILogger<ReviewSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await _context.Reviews.EstimatedDocumentCountAsync(cancellationToken: ct);
        if (existing > 0)
        {
            _logger.LogInformation("Collection {Collection} already has {Count} documents, seeding skipped",
                MongoDbContext.ReviewsCollectionName, existing);
            return;
        }

        var reviews = BuildReviews().ToList();
        await _context.Reviews.InsertManyAsync(reviews, cancellationToken: ct);

        _logger.LogInformation("Seeded {Count} documents into {Collection}",
            reviews.Count, MongoDbContext.ReviewsCollectionName);
    }

    private static IEnumerable<Review> BuildReviews()
    {
        var olena = new Author(1, "Олена К.", "Dry");
        var andrii = new Author(2, "Андрій М.", "Oily");
        var iryna = new Author(3, "Ірина С.", "Sensitive");
        var guest = new Author(null, "Гість");

        var cleanser = new Review(1, "CeraVe Hydrating Cleanser 236 ml", olena, new Rating(5),
            "Ідеально для сухої шкіри", "Не стягує шкіру після вмивання, м'яко очищає.",
            new UsageDuration(2, "months"), ["/images/reviews/crv-hcl-1.jpg"]);
        cleanser.AddComment("Ірина С.", "Дякую, візьму на пробу!");
        yield return cleanser;

        yield return new Review(1, "CeraVe Hydrating Cleanser 236 ml", iryna, new Rating(4),
            "Добре, але без ефекту вау", "Підходить для чутливої шкіри, подразнень не було.");

        yield return new Review(2, "La Roche-Posay Effaclar Duo+ 40 ml", guest, new Rating(4),
            null, "Висипань стало менше вже за два тижні.", new UsageDuration(2, "weeks"));

        yield return new Review(3, "The Ordinary Niacinamide 10% + Zinc 1% 30 ml", andrii, new Rating(5),
            "Пори помітно менші", "Шкіра менше блищить до вечора.", new UsageDuration(6, "weeks"));

        yield return new Review(5, "Neutrogena Hydro Boost Water Gel 50 ml", andrii, new Rating(3),
            "Легкий, але мало", "Добре зволожує, проте швидко закінчується.");

        yield return new Review(8, "Avène Cleanance SPF50+ 50 ml", olena, new Rating(5),
            "Не залишає білих слідів", "Гарний захист, під макіяж лягає рівно.");
    }
}
