using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Common;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence.Mappings;

public static class MongoMappings
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered)
                return;
            _registered = true;
        }

        ConventionRegistry.Register(
            "ReviewsConventions",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new IgnoreIfNullConvention(true)
            },
            type => type.Namespace?.StartsWith("Reviews.", StringComparison.Ordinal) == true);

        BsonSerializer.TryRegisterSerializer(new RatingSerializer());

        BsonClassMap.TryRegisterClassMap<BaseEntity>(cm =>
        {
            cm.MapIdMember(e => e.Id);
            cm.MapProperty(e => e.CreatedAt).SetElementName("createdAt");
            cm.MapProperty(e => e.UpdatedAt).SetElementName("updatedAt").SetIgnoreIfNull(true);
            cm.MapProperty(e => e.Version).SetElementName("version");
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.TryRegisterClassMap<Review>(cm =>
        {
            cm.MapProperty(r => r.ProductId).SetElementName("productId");
            cm.MapProperty(r => r.ProductName).SetElementName("productName");
            cm.MapProperty(r => r.Author).SetElementName("author");
            cm.MapProperty(r => r.Rating).SetElementName("rating");
            cm.MapProperty(r => r.Title).SetElementName("title").SetIgnoreIfNull(true);
            cm.MapProperty(r => r.Text).SetElementName("text");
            cm.MapProperty(r => r.UsageDuration).SetElementName("usageDuration").SetIgnoreIfNull(true);
            cm.MapProperty(r => r.DiscussionId).SetElementName("discussionId").SetIgnoreIfNull(true);
            cm.MapProperty(r => r.HelpfulVotes).SetElementName("helpfulVotes");
            cm.MapField("_comments").SetElementName("comments");
            cm.MapField("_photos").SetElementName("photos");
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.TryRegisterClassMap<Discussion>(cm =>
        {
            cm.MapProperty(d => d.ProductId).SetElementName("productId");
            cm.MapProperty(d => d.ProductName).SetElementName("productName");
            cm.MapProperty(d => d.Title).SetElementName("title");
            cm.MapProperty(d => d.CreatedBy).SetElementName("createdBy");
            cm.MapProperty(d => d.MessagesCount).SetElementName("messagesCount");
            cm.MapProperty(d => d.LastMessageAt).SetElementName("lastMessageAt").SetIgnoreIfNull(true);
            cm.MapField("_tags").SetElementName("tags");
            cm.MapField("_recentMessages").SetElementName("recentMessages");
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.TryRegisterClassMap<Author>(cm =>
        {
            cm.MapProperty(a => a.CustomerId).SetElementName("customerId")
                .SetIgnoreIfNull(true).SetDefaultValue((long?)null);
            cm.MapProperty(a => a.DisplayName).SetElementName("displayName");
            cm.MapProperty(a => a.SkinType).SetElementName("skinType")
                .SetIgnoreIfNull(true).SetDefaultValue((string?)null);
            cm.MapCreator(a => new Author(a.CustomerId, a.DisplayName, a.SkinType));
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.TryRegisterClassMap<UsageDuration>(cm =>
        {
            cm.MapProperty(u => u.Value).SetElementName("value");
            cm.MapProperty(u => u.Unit).SetElementName("unit");
            cm.MapCreator(u => new UsageDuration(u.Value, u.Unit));
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.TryRegisterClassMap<Comment>(cm =>
        {
            cm.MapProperty(c => c.AuthorName).SetElementName("authorName");
            cm.MapProperty(c => c.Text).SetElementName("text");
            cm.MapProperty(c => c.CreatedAt).SetElementName("createdAt");
            cm.MapCreator(c => new Comment(c.AuthorName, c.Text, c.CreatedAt));
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.TryRegisterClassMap<MessagePreview>(cm =>
        {
            cm.MapProperty(m => m.MessageId).SetElementName("messageId").SetDefaultValue(ObjectId.Empty);
            cm.MapProperty(m => m.AuthorName).SetElementName("authorName");
            cm.MapProperty(m => m.Text).SetElementName("text");
            cm.MapProperty(m => m.CreatedAt).SetElementName("createdAt");
            cm.MapCreator(m => new MessagePreview(m.MessageId, m.AuthorName, m.Text, m.CreatedAt));
            cm.SetIgnoreExtraElements(true);
        });

        BsonClassMap.TryRegisterClassMap<ReviewDto>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(d => d.Id).SetSerializer(new StringSerializer(BsonType.ObjectId));
            cm.MapMember(d => d.DiscussionId).SetSerializer(new StringSerializer(BsonType.ObjectId));
        });
    }
}
