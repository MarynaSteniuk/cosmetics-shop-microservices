namespace Catalog.Bll.Dtos;

public sealed record ProductsLookupDto(IReadOnlyList<ProductDto> Products, IReadOnlyList<long> MissingIds);
