using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Catalog.Api.Metrics;

public sealed class CatalogMetrics
{
    public const string MeterName = "Catalog.Api";
    public const int LowStockThreshold = 5;

    private readonly Counter<long> _productsCreated;
    private readonly Counter<long> _productsPriceChanged;
    private readonly Counter<long> _productsStockChanged;
    private readonly Counter<long> _productsDeleted;
    private readonly Histogram<double> _operationDuration;

    private long _activeOperations;
    private long _lowStockProducts;

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _productsCreated = meter.CreateCounter<long>("products.created", unit: "{product}",
            description: "Number of created products");
        _productsPriceChanged = meter.CreateCounter<long>("products.price_changed", unit: "{product}",
            description: "Number of product price changes");
        _productsStockChanged = meter.CreateCounter<long>("products.stock_changed", unit: "{product}",
            description: "Number of product stock changes");
        _productsDeleted = meter.CreateCounter<long>("products.deleted", unit: "{product}",
            description: "Number of deleted products");
        _operationDuration = meter.CreateHistogram<double>("catalog.operation.duration", unit: "ms",
            description: "Duration of catalog operations, successful and failed");

        meter.CreateObservableGauge("catalog.products.low_stock",
            () => Interlocked.Read(ref _lowStockProducts),
            unit: "{product}", description: $"Active products with stock below {LowStockThreshold}");
        meter.CreateObservableGauge("catalog.operations.active",
            () => Interlocked.Read(ref _activeOperations),
            unit: "{operation}", description: "Catalog operations being processed right now");
    }

    public void ProductCreated(bool isActive)
        => _productsCreated.Add(1, new KeyValuePair<string, object?>("product.active", isActive));

    public void PriceChanged(decimal oldPrice, decimal newPrice)
        => _productsPriceChanged.Add(1, new KeyValuePair<string, object?>("price.direction",
            newPrice > oldPrice ? "up" : newPrice < oldPrice ? "down" : "same"));

    public void StockChanged(string change)
        => _productsStockChanged.Add(1, new KeyValuePair<string, object?>("stock.change", change));

    public void ProductDeleted()
        => _productsDeleted.Add(1);

    public void SetLowStockProducts(long count)
        => Interlocked.Exchange(ref _lowStockProducts, count);

    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        Interlocked.Increment(ref _activeOperations);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            RecordDuration(operation, success: true, stopwatch.Elapsed.TotalMilliseconds);
            return result;
        }
        catch
        {
            RecordDuration(operation, success: false, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    public Task MeasureAsync(string operation, Func<Task> action)
        => MeasureAsync(operation, async () =>
        {
            await action();
            return true;
        });

    private void RecordDuration(string operation, bool success, double elapsedMs)
        => _operationDuration.Record(elapsedMs,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
}
