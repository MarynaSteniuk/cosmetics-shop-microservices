namespace Platform.Aggregator.Contracts;

public sealed record OrderDto(
    long Id,
    long CustomerId,
    string Status,
    DateTime OrderDate,
    decimal TotalAmount,
    string? ShippingAddress,
    string? City,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderItemDto(
    long ProductId,
    string ProductName,
    decimal UnitPrice,
    decimal Discount,
    int Units,
    decimal LineTotal);

public sealed record ProductDto(
    long Id,
    string Sku,
    string Name,
    string BrandName,
    decimal Price,
    int AvailableStock,
    bool IsActive);

public sealed record ReviewDto(
    string Id,
    long ProductId,
    string AuthorName,
    int Rating,
    string? Title,
    string Text,
    DateTime CreatedAt);

public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize);
