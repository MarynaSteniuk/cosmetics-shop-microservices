using FluentValidation;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Queries.GetReviewsList;

public sealed class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetReviewsListQuery.MaxPageSize);
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId is not null);
        RuleFor(x => x.MinRating).InclusiveBetween(Rating.Min, Rating.Max).When(x => x.MinRating is not null);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
