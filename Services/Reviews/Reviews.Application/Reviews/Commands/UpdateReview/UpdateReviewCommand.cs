using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed record UpdateReviewCommand(
    string Id,
    int Rating,
    string? Title,
    string Text,
    int? ExpectedVersion) : ICommand<ReviewDto>;
