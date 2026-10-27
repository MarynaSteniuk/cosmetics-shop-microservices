using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Contracts;
using Platform.Aggregator.Telemetry;

namespace Platform.Aggregator.Services;

public sealed class OrderAggregationService : IOrderAggregationService
{
    private const string OrdersService = "orders-api";
    private const string CatalogService = "catalog-api";
    private const string ReviewsService = "reviews-api";
    private const int LatestReviewsCount = 3;

    private readonly OrdersClient _orders;
    private readonly CatalogClient _catalog;
    private readonly ReviewsClient _reviews;
    private readonly ILogger<OrderAggregationService> _logger;

    public OrderAggregationService(OrdersClient orders, CatalogClient catalog, ReviewsClient reviews,
        ILogger<OrderAggregationService> logger)
    {
        _orders = orders;
        _catalog = catalog;
        _reviews = reviews;
        _logger = logger;
    }

    public async Task<FullOrderDto?> GetFullOrderAsync(long orderId, CancellationToken ct)
    {
        using var activity = AggregatorTelemetry.Source.StartActivity("AggregateFullOrder");
        activity?.SetTag("order.id", orderId);

        var warnings = new List<string>();

        OrderDto? order;
        try
        {
            order = await CallAsync(OrdersService, $"order {orderId}", () => _orders.GetOrderAsync(orderId, ct));
        }
        catch (Exception ex) when (IsDependencyFailure(ex, ct))
        {
            _logger.LogError(ex, "{Service} is unavailable, order {OrderId} cannot be aggregated", OrdersService, orderId);
            throw new DependencyUnavailableException(OrdersService, ex);
        }

        if (order is null)
            return null;

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsLabel = $"products {string.Join(", ", productIds)}";

        var productsTask = SafeCallAsync(CatalogService, productsLabel,
            () => _catalog.GetProductsAsync(productIds, ct), warnings, ct);
        var reviewsTask = SafeCallAsync(ReviewsService, productsLabel,
            () => _reviews.GetForProductsAsync(productIds, ct), warnings, ct);

        await Task.WhenAll(productsTask, reviewsTask);

        var products = (await productsTask)?
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);
        var reviews = await reviewsTask;

        if (products is not null)
            foreach (var missing in productIds.Where(id => !products.ContainsKey(id)))
                AddWarning(warnings, $"Product {missing} from order {orderId} was not found in {CatalogService}");

        var lines = order.Items.Select(item => new OrderLineCardDto(
            item.ProductId,
            item.ProductName,
            item.Units,
            item.UnitPrice,
            item.LineTotal,
            products is not null && products.TryGetValue(item.ProductId, out var product)
                ? new CatalogInfoDto(product.Sku, product.BrandName, product.Price, product.AvailableStock,
                    product.IsActive, product.Price != item.UnitPrice)
                : null,
            reviews is not null && reviews.TryGetValue(item.ProductId, out var page)
                ? ToSummary(page)
                : null)).ToList();

        activity?.SetTag("order.lines", lines.Count);
        activity?.SetTag("aggregation.warnings", warnings.Count);

        _logger.LogInformation("Order {OrderId} aggregated: {LinesCount} lines, {WarningsCount} warnings",
            orderId, lines.Count, warnings.Count);

        return new FullOrderDto(
            new OrderCardDto(order.Id, order.CustomerId, order.Status, order.OrderDate, order.TotalAmount, order.City),
            lines,
            warnings);
    }

    private async Task<T> CallAsync<T>(string service, string target, Func<Task<T>> call)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Calling {Service} for {Target}", service, target);

        var result = await call();

        _logger.LogInformation("{Service} responded in {ElapsedMs} ms", service, stopwatch.ElapsedMilliseconds);
        return result;
    }

    private async Task<T?> SafeCallAsync<T>(string service, string target, Func<Task<T>> call,
        List<string> warnings, CancellationToken ct) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await CallAsync(service, target, call);
        }
        catch (Exception ex) when (IsDependencyFailure(ex, ct))
        {
            _logger.LogWarning(ex, "{Service} is unavailable after {ElapsedMs} ms: {Reason}",
                service, stopwatch.ElapsedMilliseconds, ex.Message);
            AddWarning(warnings, $"{service} is unavailable");
            return null;
        }
    }

    private static bool IsDependencyFailure(Exception ex, CancellationToken ct)
        => ex is not OperationCanceledException || !ct.IsCancellationRequested;

    private static void AddWarning(List<string> warnings, string warning)
    {
        lock (warnings)
            warnings.Add(warning);
    }

    private static ReviewsSummaryDto ToSummary(PagedResultDto<ReviewDto> page)
        => new(
            page.TotalCount,
            page.Items.Count == 0 ? null : Math.Round(page.Items.Average(r => r.Rating), 2),
            page.Items
                .OrderByDescending(r => r.CreatedAt)
                .Take(LatestReviewsCount)
                .Select(r => new ReviewCardDto(r.Id, r.AuthorName, r.Rating, r.Title, r.Text, r.CreatedAt))
                .ToList());
}
