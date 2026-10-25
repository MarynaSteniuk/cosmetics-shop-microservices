using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.DeleteReview;

public sealed class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand>
{
    private readonly IReviewRepository _reviews;
    private readonly ILogger<DeleteReviewCommandHandler> _logger;

    public DeleteReviewCommandHandler(IReviewRepository reviews, ILogger<DeleteReviewCommandHandler> logger)
    {
        _reviews = reviews;
        _logger = logger;
    }

    public async Task Handle(DeleteReviewCommand command, CancellationToken cancellationToken)
    {
        if (!await _reviews.DeleteAsync(ObjectId.Parse(command.Id), cancellationToken))
            throw new NotFoundException(nameof(Review), command.Id);

        _logger.LogInformation("Review {ReviewId} deleted", command.Id);
    }
}
