using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public sealed class LowStockProductsSpec : Specification<Product>
{
    public LowStockProductsSpec(int threshold)
    {
        Query.AsNoTracking();
        Query.Include(p => p.Brand);
        Query.Where(p => p.IsActive && p.AvailableStock <= threshold);
        Query.OrderBy(p => p.AvailableStock).ThenBy(p => p.Name);
    }
}
