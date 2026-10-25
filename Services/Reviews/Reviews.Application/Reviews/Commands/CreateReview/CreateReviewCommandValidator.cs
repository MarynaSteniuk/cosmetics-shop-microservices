using FluentValidation;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository reviews)
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId is not null);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SkinType)
            .Must(type => Author.SkinTypes.Contains(type!))
            .When(x => x.SkinType is not null)
            .WithMessage($"Skin type must be one of: {string.Join(", ", Author.SkinTypes)}");
        RuleFor(x => x.Rating).InclusiveBetween(Rating.Min, Rating.Max);
        RuleFor(x => x.Title).MaximumLength(Review.MaxTitleLength);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Review.MaxTextLength);

        RuleFor(x => x.UsageValue).GreaterThan(0).When(x => x.UsageValue is not null);
        RuleFor(x => x.UsageUnit)
            .Must(unit => UsageDuration.Units.Contains(unit!))
            .When(x => x.UsageUnit is not null)
            .WithMessage($"Usage unit must be one of: {string.Join(", ", UsageDuration.Units)}");
        RuleFor(x => x)
            .Must(x => (x.UsageValue is null) == (x.UsageUnit is null))
            .OverridePropertyName("UsageUnit")
            .WithMessage("UsageValue and UsageUnit must be provided together");

        RuleFor(x => x.Photos!)
            .Must(photos => photos.Count <= Review.MaxPhotos)
            .When(x => x.Photos is not null)
            .WithMessage($"A review can have at most {Review.MaxPhotos} photos");
        RuleForEach(x => x.Photos).NotEmpty().MaximumLength(500);

        RuleFor(x => x)
            .MustAsync(async (command, ct) => !await reviews.ExistsAsync(command.ProductId, command.CustomerId!.Value, ct))
            .When(x => x.CustomerId is > 0 && x.ProductId > 0)
            .OverridePropertyName("CustomerId")
            .WithMessage("This customer has already reviewed this product");
    }
}
