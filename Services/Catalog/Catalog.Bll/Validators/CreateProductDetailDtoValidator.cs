using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;
using FluentValidation;

namespace Catalog.Bll.Validators;

public class CreateProductDetailDtoValidator : AbstractValidator<CreateProductDetailDto>
{
    public CreateProductDetailDtoValidator()
    {
        RuleFor(x => x.Volume)
            .GreaterThan(0)
            .PrecisionScale(8, 2, true);

        RuleFor(x => x.VolumeUnit)
            .Must(unit => VolumeUnits.Values.Contains(unit))
            .WithMessage($"Volume unit must be one of: {string.Join(", ", VolumeUnits.Values)}");

        RuleFor(x => x.SkinType)
            .Must(type => SkinTypes.Values.Contains(type))
            .WithMessage($"Skin type must be one of: {string.Join(", ", SkinTypes.Values)}");
    }
}
