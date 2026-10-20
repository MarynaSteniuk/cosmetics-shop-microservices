using Catalog.Bll.Dtos;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class ChangePriceDtoValidator : AbstractValidator<ChangePriceDto>
{
    public ChangePriceDtoValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .PrecisionScale(18, 2, true);
    }
}
