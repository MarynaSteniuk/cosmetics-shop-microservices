using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IProductCategoryRepository ProductCategories { get; }
    IGenericRepository<Brand> Brands { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
