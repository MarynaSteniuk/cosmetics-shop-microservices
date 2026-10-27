using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Common.Telemetry;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, string>
{
    private readonly IReviewRepository _reviews;
    private readonly ILogger<CreateReviewCommandHandler> _logger;

    public CreateReviewCommandHandler(IReviewRepository reviews, ILogger<CreateReviewCommandHandler> logger)
    {
        _reviews = reviews;
        _logger = logger;
    }

    public async Task<string> Handle(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        using var activity = ReviewsTelemetry.Source.StartActivity("CreateReview");
        activity?.SetTag("product.id", command.ProductId);
        activity?.SetTag("review.rating", command.Rating);

        var usageDuration = command.UsageValue is { } value && command.UsageUnit is { } unit
            ? new UsageDuration(value, unit)
            : null;

        var review = new Review(
            command.ProductId,
            command.ProductName,
            new Author(command.CustomerId, command.AuthorName, command.SkinType),
            new Rating(command.Rating),
            command.Title,
            command.Text,
            usageDuration,
            command.Photos);

        await _reviews.AddAsync(review, cancellationToken);

        activity?.SetTag("review.id", review.Id.ToString());
        _logger.LogInformation("Review {ReviewId} created for product {ProductId} with rating {Rating}",
            review.Id, review.ProductId, review.Rating.Value);
        return review.Id.ToString();
    }
}
