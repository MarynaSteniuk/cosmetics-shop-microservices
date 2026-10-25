using MongoDB.Bson;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.Entities;

public sealed class MessagePreview
{
    public ObjectId MessageId { get; }
    public string AuthorName { get; }
    public string Text { get; }
    public DateTime CreatedAt { get; }

    public MessagePreview(ObjectId messageId, string authorName, string text, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(authorName))
            throw new DomainException("Message author is required");
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Message text is required");

        MessageId = messageId;
        AuthorName = authorName.Trim();
        Text = text.Trim();
        CreatedAt = createdAt;
    }
}
