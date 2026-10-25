using Reviews.Application.Common.Models;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Common.Interfaces;

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PagedResult<ReviewDto>> GetListAsync(ReviewListFilter filter, CancellationToken ct = default);
}

public sealed record ReviewListFilter(long? ProductId, int? MinRating, string? Search, int Page, int PageSize);
