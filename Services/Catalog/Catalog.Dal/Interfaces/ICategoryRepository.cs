using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default);
}
