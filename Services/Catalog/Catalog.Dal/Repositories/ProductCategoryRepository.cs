using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductCategoryRepository : GenericRepository<ProductCategory>, IProductCategoryRepository
{
    public ProductCategoryRepository(CatalogDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ProductCategory>> GetRecentlyAddedAsync(string categorySlug, DateTime since,
        CancellationToken ct = default)
        => await Set
            .Include(pc => pc.Product).ThenInclude(p => p.Brand)
            .Include(pc => pc.Category)
            .Where(pc => pc.Category.Slug == categorySlug && pc.AddedAt >= since)
            .OrderByDescending(pc => pc.AddedAt)
            .AsNoTracking()
            .ToListAsync(ct);
}
