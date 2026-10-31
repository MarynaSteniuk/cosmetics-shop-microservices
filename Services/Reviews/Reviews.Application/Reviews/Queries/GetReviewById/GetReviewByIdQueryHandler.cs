using MediatR;
using Reviews.Application.Caching;
using Reviews.Application.Common.Exceptions;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Reviews.Dtos;
using Reviews.Domain.Entities;

namespace Reviews.Application.Reviews.Queries.GetReviewById;

public sealed class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, ReviewDto>
{
    private readonly IReviewQueries _queries;
    private readonly ITwoLevelCache _cache;

    public GetReviewByIdQueryHandler(IReviewQueries queries, ITwoLevelCache cache)
    {
        _queries = queries;
        _cache = cache;
    }

    public async Task<ReviewDto> Handle(GetReviewByIdQuery query, CancellationToken cancellationToken)
        => await _cache.GetOrCreateAsync<ReviewDto>(ReviewsCacheKeys.Review(query.Id), ReviewsCacheProfiles.Review,
               token => _queries.GetByIdAsync(query.Id, token), cancellationToken)
           ?? throw new NotFoundException(nameof(Review), query.Id);
}
