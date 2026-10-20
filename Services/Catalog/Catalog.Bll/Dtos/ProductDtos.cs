using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public class ProductListItemDto
{
    public long Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string BrandName { get; set; } = "";
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public bool IsActive { get; set; }
}

public class ProductDto
{
    public long Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public int BrandId { get; set; }
    public string BrandName { get; set; } = "";
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public ProductDetailDto? Detail { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductCategoryDto> Categories { get; set; } = new();
}

public class ProductDetailDto
{
    public string? Description { get; set; }
    public string? Ingredients { get; set; }
    public string? HowToUse { get; set; }
    public decimal Volume { get; set; }
    public string VolumeUnit { get; set; } = "";
    public string SkinType { get; set; } = "";
}

public class ProductImageDto
{
    public long Id { get; set; }
    public string Url { get; set; } = "";
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsMain { get; set; }
}

public class ProductCategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public DateTime AddedAt { get; set; }
}

public class CreateProductDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, StringLength(30)]
    public string Sku { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int BrandId { get; set; }

    public decimal Price { get; set; }

    [Range(0, 100000)]
    public int InitialStock { get; set; }

    public CreateProductDetailDto? Detail { get; set; }

    public List<int> CategoryIds { get; set; } = new();
}

public class CreateProductDetailDto
{
    [StringLength(2000)]
    public string? Description { get; set; }

    public string? Ingredients { get; set; }

    [StringLength(1000)]
    public string? HowToUse { get; set; }

    public decimal Volume { get; set; }

    [Required, StringLength(5)]
    public string VolumeUnit { get; set; } = "";

    [Required, StringLength(20)]
    public string SkinType { get; set; } = "";
}

public class ChangePriceDto
{
    public decimal Price { get; set; }
}

public class StockChangeDto
{
    [Range(1, 10000)]
    public int Quantity { get; set; }
}
