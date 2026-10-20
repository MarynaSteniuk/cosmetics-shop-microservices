using Catalog.Bll.Queries;
using Catalog.Domain.Entities;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class ProductQueryValidator : AbstractValidator<ProductQuery>
{
    public ProductQueryValidator()
    {
        RuleFor(x => x.SortBy)
            .Must(field => ProductQuery.SortFields.Contains(field, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ProductQuery.SortFields)}");

        RuleFor(x => x.SortDir)
            .Must(dir => dir.Equals("asc", StringComparison.OrdinalIgnoreCase)
                      || dir.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDir must be 'asc' or 'desc'");

        RuleFor(x => x.SkinType)
            .Must(type => SkinTypes.Values.Contains(type!))
            .When(x => !string.IsNullOrWhiteSpace(x.SkinType))
            .WithMessage($"Skin type must be one of: {string.Join(", ", SkinTypes.Values)}");

        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice is not null);

        RuleFor(x => x)
            .Must(x => x.MinPrice <= x.MaxPrice)
            .When(x => x.MinPrice is not null && x.MaxPrice is not null)
            .OverridePropertyName("MaxPrice")
            .WithMessage("MaxPrice must be greater than or equal to MinPrice");
    }
}
