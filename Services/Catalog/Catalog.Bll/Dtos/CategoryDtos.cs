namespace Catalog.Bll.Dtos;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int SortOrder { get; set; }
}

public class CategoryWithProductsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public List<ProductInCategoryDto> Products { get; set; } = new();
}

public class ProductInCategoryDto
{
    public long ProductId { get; set; }
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime AddedAt { get; set; }
}

public class RecentlyAddedProductDto
{
    public long ProductId { get; set; }
    public string Name { get; set; } = "";
    public string Sku { get; set; } = "";
    public string BrandName { get; set; } = "";
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = "";
    public DateTime AddedAt { get; set; }
}
