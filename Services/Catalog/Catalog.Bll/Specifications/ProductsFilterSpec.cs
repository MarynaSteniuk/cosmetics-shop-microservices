using Ardalis.Specification;
using Catalog.Bll.Queries;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class ProductsFilterSpec : Specification<Product>
{
    public ProductsFilterSpec(ProductQuery query, int page, int pageSize)
    {
        Query.AsNoTracking();
        Query.Include(p => p.Brand);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            Query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }

        if (query.BrandId is { } brandId)
            Query.Where(p => p.BrandId == brandId);

        if (query.CategoryId is { } categoryId)
            Query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId));

        if (!string.IsNullOrWhiteSpace(query.SkinType))
        {
            var skinType = query.SkinType;
            Query.Where(p => p.Detail != null && p.Detail.SkinType == skinType);
        }

        if (query.MinPrice is { } minPrice)
            Query.Where(p => p.Price >= minPrice);

        if (query.MaxPrice is { } maxPrice)
            Query.Where(p => p.Price <= maxPrice);

        if (query.InStock == true)
            Query.Where(p => p.AvailableStock > 0);

        var descending = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        switch (query.SortBy.ToLowerInvariant())
        {
            case "price":
                if (descending) Query.OrderByDescending(p => p.Price).ThenBy(p => p.Id);
                else Query.OrderBy(p => p.Price).ThenBy(p => p.Id);
                break;
            case "stock":
                if (descending) Query.OrderByDescending(p => p.AvailableStock).ThenBy(p => p.Id);
                else Query.OrderBy(p => p.AvailableStock).ThenBy(p => p.Id);
                break;
            case "createdat":
                if (descending) Query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id);
                else Query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id);
                break;
            default:
                if (descending) Query.OrderByDescending(p => p.Name).ThenBy(p => p.Id);
                else Query.OrderBy(p => p.Name).ThenBy(p => p.Id);
                break;
        }

        Query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}
