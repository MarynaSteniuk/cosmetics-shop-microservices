using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Category
{
    private readonly List<ProductCategory> _productCategories = new();

    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories;

    private Category() { }

    public Category(string name, string slug, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required");
        if (string.IsNullOrWhiteSpace(slug) || slug.Contains(' '))
            throw new DomainException("Slug is required and must not contain spaces");
        if (sortOrder < 0)
            throw new DomainException("Sort order must not be negative");

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
    }
}
