using Catalog.Bll.Dtos;
using Catalog.Bll.Queries;

namespace Catalog.Bll.Services;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetListAsync(ProductQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<ProductListItemDto>> GetLowStockAsync(int threshold, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ProductsLookupDto> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct = default);
    Task<ProductDto> ChangePriceAsync(long id, ChangePriceDto input, CancellationToken ct = default);
    Task<ProductDto> AddStockAsync(long id, StockChangeDto input, CancellationToken ct = default);
    Task<ProductDto> RemoveStockAsync(long id, StockChangeDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
