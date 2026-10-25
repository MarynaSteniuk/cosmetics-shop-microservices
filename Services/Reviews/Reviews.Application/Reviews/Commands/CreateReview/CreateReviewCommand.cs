using Reviews.Application.Common.Messaging;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed record CreateReviewCommand(
    long ProductId,
    string ProductName,
    long? CustomerId,
    string AuthorName,
    string? SkinType,
    int Rating,
    string? Title,
    string Text,
    int? UsageValue,
    string? UsageUnit,
    List<string>? Photos) : ICommand<string>;
