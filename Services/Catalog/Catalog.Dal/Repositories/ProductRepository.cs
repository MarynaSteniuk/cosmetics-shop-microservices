using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(CatalogDbContext context) : base(context) { }

    public Task<Product?> GetWithDetailsAsync(long id, CancellationToken ct = default)
        => Set
            .Include(p => p.Brand)
            .Include(p => p.Detail)
            .Include(p => p.Images)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default)
        => Set.AnyAsync(p => p.Sku == sku, ct);
}
