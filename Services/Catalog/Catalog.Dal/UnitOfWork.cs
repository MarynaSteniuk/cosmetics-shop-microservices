using Catalog.Dal.Interfaces;
using Catalog.Dal.Repositories;
using Catalog.Domain.Entities;

namespace Catalog.Dal;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context;
        Products = new ProductRepository(context);
        Categories = new CategoryRepository(context);
        ProductCategories = new ProductCategoryRepository(context);
        Brands = new GenericRepository<Brand>(context);
    }

    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public IProductCategoryRepository ProductCategories { get; }
    public IGenericRepository<Brand> Brands { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
