namespace Catalog.Domain.Entities;

public static class VolumeUnits
{
    public const string Milliliters = "ml";
    public const string Grams = "g";

    public static readonly IReadOnlyList<string> Values = [Milliliters, Grams];
}
