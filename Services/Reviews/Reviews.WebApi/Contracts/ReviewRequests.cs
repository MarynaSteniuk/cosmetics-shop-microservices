namespace Reviews.WebApi.Contracts;

public sealed record UpdateReviewRequest(int Rating, string? Title, string Text, int? Version);

public sealed record AddCommentRequest(string AuthorName, string Text);

public sealed record CreatedResponse(string Id);
