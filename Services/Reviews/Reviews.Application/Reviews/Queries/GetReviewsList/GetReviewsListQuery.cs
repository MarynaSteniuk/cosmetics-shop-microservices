using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public sealed record GetReviewsListQuery(
    long? ProductId,
    int? MinRating,
    string? Search,
    int Page = 1,
    int PageSize = 10) : IQuery<PagedResult<ReviewDto>>
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;
}
