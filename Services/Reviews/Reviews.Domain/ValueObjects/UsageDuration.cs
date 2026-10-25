using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class UsageDuration : ValueObject
{
    public static readonly IReadOnlyList<string> Units = ["days", "weeks", "months"];

    public int Value { get; }
    public string Unit { get; }

    public UsageDuration(int value, string unit)
    {
        if (value <= 0)
            throw new DomainException("Usage duration must be positive");
        if (!Units.Contains(unit))
            throw new DomainException($"Usage duration unit must be one of: {string.Join(", ", Units)}");

        Value = value;
        Unit = unit;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Unit;
    }

    public override string ToString() => $"{Value} {Unit}";
}
