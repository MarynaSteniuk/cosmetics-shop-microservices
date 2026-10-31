using AutoMapper;
using Catalog.Bll.Caching;
using Catalog.Bll.Dtos;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ITwoLevelCache _cache;

    public CategoryService(IUnitOfWork uow, IMapper mapper, ITwoLevelCache cache)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
        => await _cache.GetOrCreateAsync<List<CategoryDto>>(CatalogCacheKeys.Categories, CatalogCacheProfiles.Categories,
               async token =>
               {
                   var categories = await _uow.Categories.GetAllAsync(token);
                   return _mapper.Map<List<CategoryDto>>(categories.OrderBy(c => c.SortOrder));
               }, ct)
           ?? [];

    public async Task<CategoryWithProductsDto> GetWithProductsAsync(int id, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync<CategoryWithProductsDto>(CatalogCacheKeys.CategoryProducts(id), CatalogCacheProfiles.CategoryProducts,
               async token =>
               {
                   var category = await _uow.Categories.GetWithProductsAsync(id, token);
                   return category is null ? null : _mapper.Map<CategoryWithProductsDto>(category);
               }, ct)
           ?? throw new EntityNotFoundException(nameof(Category), id);

    public async Task<IReadOnlyList<RecentlyAddedProductDto>> GetRecentlyAddedAsync(string slug, DateTime? since,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ValidationException("Category slug is required");

        var from = since ?? DateTime.UtcNow.AddDays(-90);
        var links = await _uow.ProductCategories.GetRecentlyAddedAsync(slug.Trim().ToLowerInvariant(), from, ct);
        return _mapper.Map<List<RecentlyAddedProductDto>>(links);
    }
}
