using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Queries;

public class ProductQuery
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;
    public static readonly IReadOnlyList<string> SortFields = ["name", "price", "stock", "createdAt"];

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = DefaultPageSize;

    [StringLength(100)]
    public string? Search { get; set; }

    public int? BrandId { get; set; }
    public int? CategoryId { get; set; }
    public string? SkinType { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }

    public string SortBy { get; set; } = "name";
    public string SortDir { get; set; } = "asc";
}
