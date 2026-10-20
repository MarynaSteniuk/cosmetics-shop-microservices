namespace Catalog.Domain.Entities;

public class ProductCategory
{
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public DateTime AddedAt { get; private set; }

    private ProductCategory() { }

    internal ProductCategory(int categoryId, DateTime addedAt)
    {
        CategoryId = categoryId;
        AddedAt = addedAt;
    }
}
