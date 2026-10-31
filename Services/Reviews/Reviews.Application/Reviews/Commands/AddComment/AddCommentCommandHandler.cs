using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Application.Caching;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Reviews.Commands.AddComment;

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, ReviewDto>
{
    private readonly IReviewRepository _reviews;
    private readonly IMapper _mapper;
    private readonly ITwoLevelCache _cache;
    private readonly ILogger<AddCommentCommandHandler> _logger;

    public AddCommentCommandHandler(IReviewRepository reviews, IMapper mapper, ITwoLevelCache cache,
        ILogger<AddCommentCommandHandler> logger)
    {
        _reviews = reviews;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ReviewDto> Handle(AddCommentCommand command, CancellationToken cancellationToken)
    {
        var review = await _reviews.GetByIdAsync(ObjectId.Parse(command.ReviewId), cancellationToken)
            ?? throw new NotFoundException(nameof(Review), command.ReviewId);

        review.AddComment(command.AuthorName, command.Text);
        await _reviews.UpdateAsync(review, cancellationToken);

        _logger.LogInformation("Comment added to review {ReviewId}, version {Version}", command.ReviewId, review.Version);

        await _cache.InvalidateAsync(
            [ReviewsCacheKeys.Review(command.ReviewId), ReviewsCacheKeys.ProductSummary(review.ProductId)],
            "ReviewCommented", cancellationToken);

        return _mapper.Map<ReviewDto>(review);
    }
}
