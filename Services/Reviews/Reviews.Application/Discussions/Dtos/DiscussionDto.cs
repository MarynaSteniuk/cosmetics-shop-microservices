namespace Reviews.Application.Discussions.Dtos;

public class DiscussionDto
{
    public string Id { get; set; } = "";
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Title { get; set; } = "";
    public string CreatedByName { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public int MessagesCount { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public List<MessagePreviewDto> RecentMessages { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; }
}

public class MessagePreviewDto
{
    public string MessageId { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
