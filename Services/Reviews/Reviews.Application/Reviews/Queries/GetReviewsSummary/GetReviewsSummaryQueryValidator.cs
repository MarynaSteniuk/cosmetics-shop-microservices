using FluentValidation;
using Reviews.Application.Caching;

namespace Reviews.Application.Reviews.Queries.GetReviewsSummary;

public sealed class GetReviewsSummaryQueryValidator : AbstractValidator<GetReviewsSummaryQuery>
{
    public GetReviewsSummaryQueryValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.LatestCount).InclusiveBetween(1, ReviewsCacheProfiles.SummaryLatestCount);
    }
}
