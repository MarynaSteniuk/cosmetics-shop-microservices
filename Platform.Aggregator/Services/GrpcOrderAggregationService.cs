using System.Diagnostics;
using System.Text.Json;
using Catalog.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Orders.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Contracts;
using Platform.Aggregator.Telemetry;
using Reviews.Grpc;

namespace Platform.Aggregator.Services;

public sealed class GrpcOrderAggregationService : IOrderAggregationService
{
    private const string OrdersServiceName = "orders-api";
    private const string CatalogServiceName = "catalog-api";
    private const string ReviewsServiceName = "reviews-api";
    private const int LatestReviewsCount = 3;
    private static readonly TimeSpan CallDeadline = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OrdersService.OrdersServiceClient _orders;
    private readonly CatalogService.CatalogServiceClient _catalog;
    private readonly ReviewsService.ReviewsServiceClient _reviews;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GrpcOrderAggregationService> _logger;

    public GrpcOrderAggregationService(
        OrdersService.OrdersServiceClient orders,
        CatalogService.CatalogServiceClient catalog,
        ReviewsService.ReviewsServiceClient reviews,
        IDistributedCache cache,
        ILogger<GrpcOrderAggregationService> logger)
    {
        _orders = orders;
        _catalog = catalog;
        _reviews = reviews;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FullOrderDto?> GetFullOrderAsync(long orderId, CancellationToken ct)
    {
        var key = AggregatorCacheKeys.FullOrder(orderId);

        var cached = await ReadCacheAsync(key, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit {CacheKey}, gRPC calls skipped", key);
            return cached;
        }

        _logger.LogInformation("Cache miss {CacheKey}, aggregating over gRPC", key);

        var result = await AggregateAsync(orderId, ct);
        if (result is null)
            return null;

        if (result.Warnings.Count == 0)
            await WriteCacheAsync(key, result, ct);
        else
            _logger.LogInformation("Partial response for order {OrderId} is not cached, {WarningsCount} warnings",
                orderId, result.Warnings.Count);

        return result;
    }

    public async Task<FullOrderDto?> AggregateAsync(long orderId, CancellationToken ct)
    {
        using var activity = AggregatorTelemetry.Source.StartActivity("AggregateFullOrderGrpc");
        activity?.SetTag("order.id", orderId);

        var warnings = new List<string>();

        OrderReply order;
        try
        {
            order = await CallAsync(OrdersServiceName, $"order {orderId}",
                () => _orders.GetOrderAsync(new GetOrderRequest { Id = orderId },
                    deadline: Deadline(), cancellationToken: ct).ResponseAsync);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex) when (IsDependencyFailure(ex, ct))
        {
            _logger.LogError(ex, "{Service} is unavailable ({GrpcStatus}), order {OrderId} cannot be aggregated",
                OrdersServiceName, ex.StatusCode, orderId);
            throw new DependencyUnavailableException(OrdersServiceName, ex);
        }

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsLabel = $"products {string.Join(", ", productIds)}";

        var productsTask = SafeCallAsync(CatalogServiceName, productsLabel,
            () => _catalog.GetProductsByIdsAsync(new GetProductsByIdsRequest { Ids = { productIds } },
                deadline: Deadline(), cancellationToken: ct).ResponseAsync,
            warnings, ct);

        var reviewsTask = SafeCallAsync(ReviewsServiceName, productsLabel,
            () => _reviews.GetReviewsSummaryForProductsAsync(
                new GetReviewsSummaryRequest { ProductIds = { productIds }, LatestCount = LatestReviewsCount },
                deadline: Deadline(), cancellationToken: ct).ResponseAsync,
            warnings, ct);

        await Task.WhenAll(productsTask, reviewsTask);

        var productsReply = await productsTask;
        var reviewsReply = await reviewsTask;

        var products = productsReply?.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        if (productsReply is not null)
            foreach (var missing in productsReply.MissingIds)
                AddWarning(warnings, $"Product {missing} from order {orderId} was not found in {CatalogServiceName}");

        var summaries = reviewsReply?.Summaries
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionary(s => s.ProductId);

        var lines = order.Items.Select(item => new OrderLineCardDto(
            item.ProductId,
            item.ProductName,
            item.Units,
            ToMoney(item.UnitPrice),
            ToMoney(item.LineTotal),
            products is not null && products.TryGetValue(item.ProductId, out var product)
                ? new CatalogInfoDto(product.Sku, product.BrandName, ToMoney(product.Price), product.AvailableStock,
                    product.IsActive, ToMoney(product.Price) != ToMoney(item.UnitPrice))
                : null,
            summaries is not null && summaries.TryGetValue(item.ProductId, out var summary)
                ? ToSummary(summary)
                : null)).ToList();

        activity?.SetTag("order.lines", lines.Count);
        activity?.SetTag("aggregation.warnings", warnings.Count);

        _logger.LogInformation("Order {OrderId} aggregated over gRPC: {LinesCount} lines, {WarningsCount} warnings",
            orderId, lines.Count, warnings.Count);

        return new FullOrderDto(
            new OrderCardDto(order.Id, order.CustomerId, order.Status, order.OrderDate.ToDateTime(),
                ToMoney(order.TotalAmount), string.IsNullOrEmpty(order.City) ? null : order.City),
            lines,
            warnings);
    }

    private async Task<T> CallAsync<T>(string service, string target, Func<Task<T>> call)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Calling {Service} over gRPC for {Target}", service, target);

        var result = await call();

        _logger.LogInformation("{Service} responded over gRPC in {ElapsedMs} ms", service, stopwatch.ElapsedMilliseconds);
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
        catch (RpcException ex) when (IsDependencyFailure(ex, ct))
        {
            _logger.LogWarning(ex, "{Service} is unavailable after {ElapsedMs} ms: {GrpcStatus} {GrpcDetail}",
                service, stopwatch.ElapsedMilliseconds, ex.StatusCode, ex.Status.Detail);
            AddWarning(warnings, $"{service} is unavailable");
            return null;
        }
    }

    private static bool IsDependencyFailure(RpcException ex, CancellationToken ct)
        => ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Internal
           || (ex.StatusCode == StatusCode.Cancelled && !ct.IsCancellationRequested);

    private static DateTime Deadline() => DateTime.UtcNow.Add(CallDeadline);

    private static decimal ToMoney(double value) => Math.Round((decimal)value, 2);

    private static void AddWarning(List<string> warnings, string warning)
    {
        lock (warnings)
            warnings.Add(warning);
    }

    private static ReviewsSummaryDto ToSummary(ProductReviewsSummary summary)
        => new(
            summary.TotalCount,
            summary.HasAverageRating ? summary.AverageRating : null,
            summary.Latest
                .Select(r => new ReviewCardDto(r.Id, r.AuthorName, r.Rating, r.HasTitle ? r.Title : null, r.Text,
                    r.CreatedAt.ToDateTime()))
                .ToList());

    private async Task<FullOrderDto?> ReadCacheAsync(string key, CancellationToken ct)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            return bytes is null ? null : JsonSerializer.Deserialize<FullOrderDto>(bytes, JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis is unavailable, {CacheKey} is aggregated without cache", key);
            return null;
        }
    }

    private async Task WriteCacheAsync(string key, FullOrderDto result, CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = AggregatorCacheSettings.FullOrderTtl },
                ct);
            _logger.LogInformation("Cache set {CacheKey}, TTL {TtlSeconds} s", key,
                AggregatorCacheSettings.FullOrderTtl.TotalSeconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis is unavailable, {CacheKey} was not cached", key);
        }
    }
}
