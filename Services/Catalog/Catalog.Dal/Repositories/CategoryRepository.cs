using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(CatalogDbContext context) : base(context) { }

    public async Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default)
    {
        var category = await Set.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return null;

        await Context.Entry(category)
            .Collection(c => c.ProductCategories)
            .Query()
            .Include(pc => pc.Product)
            .LoadAsync(ct);

        return category;
    }
}
