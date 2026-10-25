using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public sealed class GetReviewsListQueryHandler : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewDto>>
{
    private readonly IReviewQueries _queries;

    public GetReviewsListQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public Task<PagedResult<ReviewDto>> Handle(GetReviewsListQuery query, CancellationToken cancellationToken)
        => _queries.GetListAsync(
            new ReviewListFilter(
                query.ProductId,
                query.MinRating,
                string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
                Math.Max(query.Page, 1),
                Math.Clamp(query.PageSize, 1, GetReviewsListQuery.MaxPageSize)),
            cancellationToken);
}
