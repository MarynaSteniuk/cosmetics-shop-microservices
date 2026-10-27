using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Common.Telemetry;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _reviews;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateReviewCommandHandler> _logger;

    public UpdateReviewCommandHandler(IReviewRepository reviews, IMapper mapper, ILogger<UpdateReviewCommandHandler> logger)
    {
        _reviews = reviews;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ReviewDto> Handle(UpdateReviewCommand command, CancellationToken cancellationToken)
    {
        using var activity = ReviewsTelemetry.Source.StartActivity("UpdateReview");
        activity?.SetTag("review.id", command.Id);
        activity?.SetTag("review.rating", command.Rating);

        var review = await _reviews.GetByIdAsync(ObjectId.Parse(command.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Review), command.Id);

        if (command.ExpectedVersion is { } expected && expected != review.Version)
            throw new ConflictException(
                $"Review {command.Id} has version {review.Version}, but version {expected} was expected");

        review.Edit(new Rating(command.Rating), command.Title, command.Text);
        await _reviews.UpdateAsync(review, cancellationToken);

        _logger.LogInformation("Review {ReviewId} updated to version {Version} with rating {Rating}",
            command.Id, review.Version, command.Rating);
        return _mapper.Map<ReviewDto>(review);
    }
}
