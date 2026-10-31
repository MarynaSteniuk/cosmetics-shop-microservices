using MediatR;
using Reviews.Application.Caching;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand>
{
    private readonly IReviewRepository _reviews;
    private readonly ITwoLevelCache _cache;
    private readonly ILogger<DeleteReviewCommandHandler> _logger;

    public DeleteReviewCommandHandler(IReviewRepository reviews, ITwoLevelCache cache,
        ILogger<DeleteReviewCommandHandler> logger)
    {
        _reviews = reviews;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(DeleteReviewCommand command, CancellationToken cancellationToken)
    {
        var id = ObjectId.Parse(command.Id);
        var review = await _reviews.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Review), command.Id);

        if (!await _reviews.DeleteAsync(id, cancellationToken))
            throw new NotFoundException(nameof(Review), command.Id);

        _logger.LogInformation("Review {ReviewId} deleted", command.Id);

        await _cache.InvalidateAsync(
            [ReviewsCacheKeys.Review(command.Id), ReviewsCacheKeys.ProductSummary(review.ProductId)],
            "ReviewDeleted", cancellationToken);
    }
}
