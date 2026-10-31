using Reviews.Application.Common.Messaging;
using Reviews.Application.Reviews.Dtos;

namespace Reviews.Application.Reviews.Queries.GetReviewsSummary;

public sealed record GetReviewsSummaryQuery(long ProductId, int LatestCount) : IQuery<ReviewsSummaryDto>;
