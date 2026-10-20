using Catalog.Bll.Dtos;

namespace Catalog.Bll.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryWithProductsDto> GetWithProductsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<RecentlyAddedProductDto>> GetRecentlyAddedAsync(string slug, DateTime? since,
        CancellationToken ct = default);
}
