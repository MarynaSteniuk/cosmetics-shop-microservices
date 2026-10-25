namespace Reviews.Application.Reviews.Dtos;

public class ReviewDto
{
    public string Id { get; set; } = "";
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public long? AuthorCustomerId { get; set; }
    public string AuthorName { get; set; } = "";
    public string? SkinType { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Text { get; set; } = "";
    public string? UsageDuration { get; set; }
    public List<string> Photos { get; set; } = new();
    public int CommentsCount { get; set; }
    public List<CommentDto> Comments { get; set; } = new();
    public string? DiscussionId { get; set; }
    public int HelpfulVotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int Version { get; set; }
}

public class CommentDto
{
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
