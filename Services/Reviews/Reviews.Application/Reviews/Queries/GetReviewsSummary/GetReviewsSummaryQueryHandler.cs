using MediatR;
using Reviews.Application.Caching;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewsSummary;

public sealed class GetReviewsSummaryQueryHandler : IRequestHandler<GetReviewsSummaryQuery, ReviewsSummaryDto>
{
    private readonly IReviewQueries _queries;
    private readonly ITwoLevelCache _cache;

    public GetReviewsSummaryQueryHandler(IReviewQueries queries, ITwoLevelCache cache)
    {
        _queries = queries;
        _cache = cache;
    }

    public async Task<ReviewsSummaryDto> Handle(GetReviewsSummaryQuery query, CancellationToken cancellationToken)
    {
        var summary = await _cache.GetOrCreateAsync<ReviewsSummaryDto>(
                          ReviewsCacheKeys.ProductSummary(query.ProductId),
                          ReviewsCacheProfiles.ProductSummary,
                          async token => await _queries.GetSummaryAsync(
                              query.ProductId, ReviewsCacheProfiles.SummaryLatestCount, token),
                          cancellationToken)
                      ?? new ReviewsSummaryDto { ProductId = query.ProductId };

        return new ReviewsSummaryDto
        {
            ProductId = summary.ProductId,
            TotalCount = summary.TotalCount,
            AverageRating = summary.AverageRating,
            Latest = summary.Latest.Take(query.LatestCount).ToList()
        };
    }
}
