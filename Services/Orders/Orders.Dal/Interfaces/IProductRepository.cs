using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task DecreaseStockAsync(long productId, int units, CancellationToken ct = default);
    Task<CatalogSyncResult> ApplyCatalogChangeAsync(long productId, string name, decimal? price, DateTime changedAt,
        CancellationToken ct = default);
}
