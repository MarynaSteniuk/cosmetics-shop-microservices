using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Brand
{
    private readonly List<Product> _products = new();

    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string? Country { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Product> Products => _products;

    private Brand() { }

    public Brand(string name, string? country)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Brand name is required");

        Name = name.Trim();
        Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Brand name is required");

        Name = name.Trim();
    }
}
