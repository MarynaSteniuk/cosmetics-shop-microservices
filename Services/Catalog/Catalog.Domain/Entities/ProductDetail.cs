using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class ProductDetail
{
    public long ProductId { get; private set; }
    public string? Description { get; private set; }
    public string? Ingredients { get; private set; }
    public string? HowToUse { get; private set; }
    public decimal Volume { get; private set; }
    public string VolumeUnit { get; private set; } = VolumeUnits.Milliliters;
    public string SkinType { get; private set; } = SkinTypes.All;

    private ProductDetail() { }

    public ProductDetail(string? description, string? ingredients, string? howToUse,
        decimal volume, string volumeUnit, string skinType)
    {
        if (volume <= 0)
            throw new DomainException("Volume must be positive");
        if (!VolumeUnits.Values.Contains(volumeUnit))
            throw new DomainException($"Volume unit must be one of: {string.Join(", ", VolumeUnits.Values)}");
        if (!SkinTypes.Values.Contains(skinType))
            throw new DomainException($"Skin type must be one of: {string.Join(", ", SkinTypes.Values)}");

        Description = description;
        Ingredients = ingredients;
        HowToUse = howToUse;
        Volume = volume;
        VolumeUnit = volumeUnit;
        SkinType = skinType;
    }
}
