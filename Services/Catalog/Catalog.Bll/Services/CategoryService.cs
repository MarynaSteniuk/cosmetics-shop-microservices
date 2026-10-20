using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CategoryService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _uow.Categories.GetAllAsync(ct);
        return _mapper.Map<List<CategoryDto>>(categories.OrderBy(c => c.SortOrder));
    }

    public async Task<CategoryWithProductsDto> GetWithProductsAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetWithProductsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);
        return _mapper.Map<CategoryWithProductsDto>(category);
    }

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
