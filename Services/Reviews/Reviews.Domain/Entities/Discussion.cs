using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public class Discussion : BaseEntity
{
    public const int RecentMessagesLimit = 3;

    private List<string> _tags = new();
    private List<MessagePreview> _recentMessages = new();

    public long ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public string Title { get; private set; } = "";
    public Author CreatedBy { get; private set; } = null!;
    public int MessagesCount { get; private set; }
    public DateTime? LastMessageAt { get; private set; }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<MessagePreview> RecentMessages => _recentMessages.AsReadOnly();

    private Discussion() { }

    public Discussion(long productId, string productName, string title, Author createdBy, IEnumerable<string>? tags = null)
    {
        if (productId <= 0)
            throw new DomainException("Product id must be positive");
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required");
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Discussion title is required");

        ProductId = productId;
        ProductName = productName.Trim();
        Title = title.Trim();
        CreatedBy = createdBy ?? throw new DomainException("Discussion author is required");

        foreach (var tag in tags ?? Enumerable.Empty<string>())
            AddTag(tag);
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new DomainException("Tag is required");

        var normalized = tag.Trim().ToLowerInvariant();
        if (!_tags.Contains(normalized))
            _tags.Add(normalized);
    }

    public void RegisterMessage(ObjectId messageId, string authorName, string text, DateTime createdAt)
    {
        _recentMessages.Add(new MessagePreview(messageId, authorName, text, createdAt));

        while (_recentMessages.Count > RecentMessagesLimit)
            _recentMessages.RemoveAt(0);

        MessagesCount++;
        LastMessageAt = createdAt;
        Touch();
    }
}
