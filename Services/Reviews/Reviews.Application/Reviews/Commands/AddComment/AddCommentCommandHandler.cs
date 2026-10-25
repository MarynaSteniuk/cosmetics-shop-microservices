using AutoMapper;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.AddComment;

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, ReviewDto>
{
    private readonly IReviewRepository _reviews;
    private readonly IMapper _mapper;

    public AddCommentCommandHandler(IReviewRepository reviews, IMapper mapper)
    {
        _reviews = reviews;
        _mapper = mapper;
    }

    public async Task<ReviewDto> Handle(AddCommentCommand command, CancellationToken cancellationToken)
    {
        var review = await _reviews.GetByIdAsync(ObjectId.Parse(command.ReviewId), cancellationToken)
            ?? throw new NotFoundException(nameof(Review), command.ReviewId);

        review.AddComment(command.AuthorName, command.Text);
        await _reviews.UpdateAsync(review, cancellationToken);

        return _mapper.Map<ReviewDto>(review);
    }
}
