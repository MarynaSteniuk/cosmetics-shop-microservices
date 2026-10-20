using Catalog.Bll.Dtos;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Sku)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("SKU may contain only letters, digits and hyphens");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .PrecisionScale(18, 2, true);

        RuleForEach(x => x.CategoryIds).GreaterThan(0);

        RuleFor(x => x.CategoryIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Category ids must not repeat");

        RuleFor(x => x.Detail!)
            .SetValidator(new CreateProductDetailDtoValidator())
            .When(x => x.Detail is not null);
    }
}
