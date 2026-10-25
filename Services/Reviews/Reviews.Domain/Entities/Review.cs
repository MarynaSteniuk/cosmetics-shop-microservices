using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public class Review : BaseEntity
{
    public const int MaxTextLength = 2000;
    public const int MaxTitleLength = 200;
    public const int MaxPhotos = 10;

    private List<Comment> _comments = new();
    private List<string> _photos = new();

    public long ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public Author Author { get; private set; } = null!;
    public Rating Rating { get; private set; } = null!;
    public string? Title { get; private set; }
    public string Text { get; private set; } = "";
    public UsageDuration? UsageDuration { get; private set; }
    public ObjectId? DiscussionId { get; private set; }
    public int HelpfulVotes { get; private set; }

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<string> Photos => _photos.AsReadOnly();

    private Review() { }

    public Review(long productId, string productName, Author author, Rating rating,
        string? title, string text, UsageDuration? usageDuration = null, IEnumerable<string>? photos = null)
    {
        if (productId <= 0)
            throw new DomainException("Product id must be positive");
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required");

        ProductId = productId;
        ProductName = productName.Trim();
        Author = author ?? throw new DomainException("Author is required");
        UsageDuration = usageDuration;

        SetContent(rating, title, text);

        foreach (var photo in photos ?? Enumerable.Empty<string>())
            AttachPhoto(photo);
    }

    public void Edit(Rating rating, string? title, string text)
    {
        SetContent(rating, title, text);
        Touch();
    }

    public void AddComment(string authorName, string text)
    {
        _comments.Add(new Comment(authorName, text, DateTime.UtcNow));
        Touch();
    }

    public void AddPhoto(string url)
    {
        AttachPhoto(url);
        Touch();
    }

    private void AttachPhoto(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Photo URL is required");
        if (_photos.Count >= MaxPhotos)
            throw new DomainException($"A review can have at most {MaxPhotos} photos");
        if (_photos.Contains(url))
            throw new DomainException($"Photo '{url}' is already attached");

        _photos.Add(url.Trim());
    }

    public void LinkDiscussion(ObjectId discussionId)
    {
        if (discussionId == ObjectId.Empty)
            throw new DomainException("Discussion id is required");

        DiscussionId = discussionId;
        Touch();
    }

    public void MarkHelpful()
    {
        HelpfulVotes++;
        Touch();
    }

    private void SetContent(Rating rating, string? title, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Review text is required");
        if (text.Trim().Length > MaxTextLength)
            throw new DomainException($"Review text must not exceed {MaxTextLength} characters");
        if (title is not null && title.Trim().Length > MaxTitleLength)
            throw new DomainException($"Review title must not exceed {MaxTitleLength} characters");

        Rating = rating ?? throw new DomainException("Rating is required");
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        Text = text.Trim();
    }
}
