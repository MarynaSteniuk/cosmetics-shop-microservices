using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Catalog.Domain.Exceptions;
using Catalog.Grpc;
using Grpc.Core;

namespace Catalog.Api.Grpc;

public sealed class CatalogGrpcService : CatalogService.CatalogServiceBase
{
    private const int MaxIdsPerRequest = 100;

    private readonly IProductService _products;

    public CatalogGrpcService(IProductService products)
    {
        _products = products;
    }

    public override async Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product id must be positive"));

        try
        {
            var product = await _products.GetByIdAsync(request.Id, context.CancellationToken);
            return ToReply(product);
        }
        catch (EntityNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ProductsReply> GetProductsByIds(GetProductsByIdsRequest request, ServerCallContext context)
    {
        if (request.Ids.Count == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one product id is required"));

        if (request.Ids.Count > MaxIdsPerRequest)
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"No more than {MaxIdsPerRequest} product ids per request"));

        if (request.Ids.Any(id => id <= 0))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product ids must be positive"));

        var result = await _products.GetByIdsAsync(request.Ids.ToList(), context.CancellationToken);

        var reply = new ProductsReply();
        reply.Products.AddRange(result.Products.Select(ToReply));
        reply.MissingIds.AddRange(result.MissingIds);
        return reply;
    }

    private static ProductReply ToReply(ProductDto product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Name = product.Name,
        BrandName = product.BrandName,
        Price = (double)product.Price,
        AvailableStock = product.AvailableStock,
        IsActive = product.IsActive
    };
}
