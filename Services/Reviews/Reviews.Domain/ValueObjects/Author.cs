using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public static readonly IReadOnlyList<string> SkinTypes = ["Normal", "Dry", "Oily", "Combination", "Sensitive"];

    public long? CustomerId { get; }
    public string DisplayName { get; }
    public string? SkinType { get; }

    public bool IsGuest => CustomerId is null;

    public Author(long? customerId, string displayName, string? skinType = null)
    {
        if (customerId is <= 0)
            throw new DomainException("Customer id must be positive");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Author display name is required");
        if (displayName.Trim().Length > 100)
            throw new DomainException("Author display name must not exceed 100 characters");
        if (skinType is not null && !SkinTypes.Contains(skinType))
            throw new DomainException($"Skin type must be one of: {string.Join(", ", SkinTypes)}");

        CustomerId = customerId;
        DisplayName = displayName.Trim();
        SkinType = skinType;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CustomerId;
        yield return DisplayName;
        yield return SkinType;
    }
}
