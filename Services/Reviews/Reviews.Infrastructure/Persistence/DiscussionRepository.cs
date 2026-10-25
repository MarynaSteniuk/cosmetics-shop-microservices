using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public sealed class DiscussionRepository : IDiscussionRepository
{
    private readonly IMongoCollection<Discussion> _discussions;

    public DiscussionRepository(MongoDbContext context)
    {
        _discussions = context.Discussions;
    }

    public async Task<Discussion?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
        => await _discussions.Find(d => d.Id == id).FirstOrDefaultAsync(ct);
}
