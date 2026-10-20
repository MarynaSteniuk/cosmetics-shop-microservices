using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Product
{
    private readonly List<ProductImage> _images = new();
    private readonly List<ProductCategory> _productCategories = new();

    public long Id { get; private set; }
    public int BrandId { get; private set; }
    public Brand Brand { get; private set; } = null!;
    public string Sku { get; private set; } = "";
    public string Name { get; private set; } = "";
    public decimal Price { get; private set; }
    public int AvailableStock { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ProductDetail? Detail { get; private set; }
    public IReadOnlyCollection<ProductImage> Images => _images;
    public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories;

    private Product() { }

    public Product(int brandId, string sku, string name, decimal price)
    {
        if (brandId <= 0)
            throw new DomainException("Brand is required");
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU is required");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");
        if (price <= 0)
            throw new DomainException("Price must be positive");

        BrandId = brandId;
        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Price = price;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");

        Name = name.Trim();
        Touch();
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new DomainException("Price must be positive");

        Price = newPrice;
        Touch();
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        AvailableStock += quantity;
        Touch();
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
        if (AvailableStock < quantity)
            throw new DomainException($"Only {AvailableStock} units of '{Sku}' available");

        AvailableStock -= quantity;
        Touch();
    }

    public void SetDetail(ProductDetail detail)
    {
        Detail = detail ?? throw new DomainException("Product detail is required");
        Touch();
    }

    public void AddImage(string url, string? altText, bool isMain)
    {
        if (_images.Any(i => i.Url == url))
            throw new DomainException($"Image '{url}' is already attached");

        if (isMain)
            foreach (var image in _images)
                image.UnmarkMain();

        _images.Add(new ProductImage(url, altText, _images.Count, isMain || _images.Count == 0));
        Touch();
    }

    public void AddToCategory(int categoryId, DateTime? addedAt = null)
    {
        if (_productCategories.Any(pc => pc.CategoryId == categoryId))
            throw new DomainException($"Product is already in category {categoryId}");

        _productCategories.Add(new ProductCategory(categoryId, addedAt ?? DateTime.UtcNow));
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
