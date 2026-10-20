using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface IProductCategoryRepository : IGenericRepository<ProductCategory>
{
    Task<IReadOnlyList<ProductCategory>> GetRecentlyAddedAsync(string categorySlug, DateTime since,
        CancellationToken ct = default);
}
