using AutoMapper;
using Orders.Bll.Dtos;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;

namespace Orders.Bll.Services;

public sealed class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await _uow.Products.GetAllAsync(ct);
        return _mapper.Map<List<ProductDto>>(products);
    }

    public async Task<ProductDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Product {id} not found");
        return _mapper.Map<ProductDto>(product);
    }
}
