namespace Catalog.Domain.Entities;

public static class SkinTypes
{
    public const string All = "All";
    public const string Normal = "Normal";
    public const string Dry = "Dry";
    public const string Oily = "Oily";
    public const string Combination = "Combination";
    public const string Sensitive = "Sensitive";

    public static readonly IReadOnlyList<string> Values = [All, Normal, Dry, Oily, Combination, Sensitive];
}
