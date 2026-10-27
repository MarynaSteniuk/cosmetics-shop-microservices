namespace Platform.Aggregator.Contracts;

public sealed record FullOrderDto(
    OrderCardDto Order,
    IReadOnlyList<OrderLineCardDto> Lines,
    IReadOnlyList<string> Warnings);

public sealed record OrderCardDto(
    long Id,
    long CustomerId,
    string Status,
    DateTime OrderDate,
    decimal TotalAmount,
    string? City);

public sealed record OrderLineCardDto(
    long ProductId,
    string ProductName,
    int Units,
    decimal OrderedUnitPrice,
    decimal LineTotal,
    CatalogInfoDto? Catalog,
    ReviewsSummaryDto? Reviews);

public sealed record CatalogInfoDto(
    string Sku,
    string BrandName,
    decimal CurrentPrice,
    int AvailableStock,
    bool IsActive,
    bool PriceChangedSinceOrder);

public sealed record ReviewsSummaryDto(
    long TotalCount,
    double? AverageRating,
    IReadOnlyList<ReviewCardDto> Latest);

public sealed record ReviewCardDto(
    string Id,
    string AuthorName,
    int Rating,
    string? Title,
    string Text,
    DateTime CreatedAt);
