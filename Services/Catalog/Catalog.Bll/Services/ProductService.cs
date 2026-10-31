using AutoMapper;
using Catalog.Bll.Caching;
using Catalog.Bll.Dtos;
using Catalog.Bll.Queries;
using Catalog.Bll.Specifications;
using Catalog.Bll.Telemetry;
using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Services;

public sealed class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ITwoLevelCache _cache;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork uow, IMapper mapper, ITwoLevelCache cache, ILogger<ProductService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<ProductListItemDto>> GetListAsync(ProductQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, ProductQuery.MaxPageSize);
        var spec = new ProductsFilterSpec(query, page, pageSize);

        var totalCount = await _uow.Products.CountAsync(spec, ct);
        var products = await _uow.Products.ListAsync(spec, ct);

        return new PagedResult<ProductListItemDto>(
            _mapper.Map<List<ProductListItemDto>>(products), totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetLowStockAsync(int threshold, CancellationToken ct = default)
    {
        if (threshold is < 0 or > 1000)
            throw new ValidationException("Threshold must be between 0 and 1000");

        var products = await _uow.Products.ListAsync(new LowStockProductsSpec(threshold), ct);
        return _mapper.Map<List<ProductListItemDto>>(products);
    }

    public async Task<ProductDto> GetByIdAsync(long id, CancellationToken ct = default)
        => await _cache.GetOrCreateAsync<ProductDto>(CatalogCacheKeys.Product(id), CatalogCacheProfiles.Product,
               async token =>
               {
                   var product = await _uow.Products.GetWithDetailsAsync(id, token);
                   return product is null ? null : _mapper.Map<ProductDto>(product);
               }, ct)
           ?? throw new EntityNotFoundException(nameof(Product), id);

    public async Task<ProductsLookupDto> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
    {
        var products = new List<ProductDto>();
        var missing = new List<long>();

        foreach (var id in ids.Distinct())
        {
            try
            {
                products.Add(await GetByIdAsync(id, ct));
            }
            catch (EntityNotFoundException)
            {
                missing.Add(id);
            }
        }

        return new ProductsLookupDto(products, missing);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct = default)
    {
        var sku = input.Sku.Trim().ToUpperInvariant();

        using var activity = CatalogTelemetry.Source.StartActivity("CreateProduct");
        activity?.SetTag("product.sku", sku);
        activity?.SetTag("brand.id", input.BrandId);
        if (await _uow.Products.ExistsBySkuAsync(sku, ct))
            throw new ConflictException($"Product with SKU '{sku}' already exists");

        if (await _uow.Brands.GetByIdAsync(input.BrandId, ct) is null)
            throw new ValidationException($"Brand {input.BrandId} does not exist");

        foreach (var categoryId in input.CategoryIds)
            if (await _uow.Categories.GetByIdAsync(categoryId, ct) is null)
                throw new ValidationException($"Category {categoryId} does not exist");

        var product = new Product(input.BrandId, sku, input.Name, input.Price);

        if (input.InitialStock > 0)
            product.AddStock(input.InitialStock);

        if (input.Detail is { } detail)
            product.SetDetail(new ProductDetail(detail.Description, detail.Ingredients, detail.HowToUse,
                detail.Volume, detail.VolumeUnit, detail.SkinType));

        foreach (var categoryId in input.CategoryIds)
            product.AddToCategory(categoryId);

        await _uow.Products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        activity?.SetTag("product.id", product.Id);
        _logger.LogInformation("Product {ProductId} created with SKU {Sku}, price {Price}, stock {Stock}",
            product.Id, product.Sku, product.Price, product.AvailableStock);

        await _cache.InvalidateAsync(input.CategoryIds.Select(CatalogCacheKeys.CategoryProducts), "ProductCreated", ct);
        return await GetByIdAsync(product.Id, ct);
    }

    public async Task<ProductDto> ChangePriceAsync(long id, ChangePriceDto input, CancellationToken ct = default)
    {
        using var activity = CatalogTelemetry.Source.StartActivity("ChangeProductPrice");
        activity?.SetTag("product.id", id);
        activity?.SetTag("product.new_price", input.Price);

        var product = await FindAsync(id, ct);
        var oldPrice = product.Price;

        product.ChangePrice(input.Price);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} price changed from {OldPrice} to {NewPrice}",
            id, oldPrice, input.Price);

        await InvalidateProductAsync(product, "ProductPriceChanged", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<ProductDto> AddStockAsync(long id, StockChangeDto input, CancellationToken ct = default)
    {
        var product = await FindAsync(id, ct);

        product.AddStock(input.Quantity);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} stock increased by {Quantity}", id, input.Quantity);

        await InvalidateProductAsync(product, "ProductStockChanged", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<ProductDto> RemoveStockAsync(long id, StockChangeDto input, CancellationToken ct = default)
    {
        using var activity = CatalogTelemetry.Source.StartActivity("RemoveProductStock");
        activity?.SetTag("product.id", id);
        activity?.SetTag("stock.quantity", input.Quantity);

        var product = await FindAsync(id, ct);

        product.RemoveStock(input.Quantity);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} stock decreased by {Quantity}", id, input.Quantity);

        await InvalidateProductAsync(product, "ProductStockChanged", ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var product = await FindAsync(id, ct);
        var categoryIds = product.ProductCategories.Select(pc => pc.CategoryId).ToList();

        _uow.Products.Delete(product);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} deleted", id);

        await _cache.InvalidateAsync(
            categoryIds.Select(CatalogCacheKeys.CategoryProducts).Prepend(CatalogCacheKeys.Product(id)),
            "ProductDeleted", ct);
    }

    private async Task<Product> FindAsync(long id, CancellationToken ct)
        => await _uow.Products.GetWithDetailsAsync(id, ct)
           ?? throw new EntityNotFoundException(nameof(Product), id);

    private Task InvalidateProductAsync(Product product, string reason, CancellationToken ct)
        => _cache.InvalidateAsync(
            product.ProductCategories
                .Select(pc => CatalogCacheKeys.CategoryProducts(pc.CategoryId))
                .Prepend(CatalogCacheKeys.Product(product.Id)),
            reason, ct);
}
