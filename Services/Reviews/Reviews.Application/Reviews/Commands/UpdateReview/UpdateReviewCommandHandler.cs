using AutoMapper;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _reviews;
    private readonly IMapper _mapper;

    public UpdateReviewCommandHandler(IReviewRepository reviews, IMapper mapper)
    {
        _reviews = reviews;
        _mapper = mapper;
    }

    public async Task<ReviewDto> Handle(UpdateReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await _reviews.GetByIdAsync(ObjectId.Parse(command.Id), cancellationToken)
            ?? throw new NotFoundException(nameof(Review), command.Id);

        if (command.ExpectedVersion is { } expected && expected != review.Version)
            throw new ConflictException(
                $"Review {command.Id} has version {review.Version}, but version {expected} was expected");

        review.Edit(new Rating(command.Rating), command.Title, command.Text);
        await _reviews.UpdateAsync(review, cancellationToken);

        return _mapper.Map<ReviewDto>(review);
    }
}
