using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetWithDetailsAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default);
}
