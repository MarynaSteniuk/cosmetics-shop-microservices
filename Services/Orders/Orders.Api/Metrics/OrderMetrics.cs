using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.Api.Metrics;

public sealed class OrderMetrics
{
    public const string MeterName = "Orders.Api";

    private readonly Counter<long> _ordersCreated;
    private readonly Counter<long> _ordersConfirmed;
    private readonly Counter<long> _ordersStatusChanged;
    private readonly Counter<long> _ordersDeleted;
    private readonly Histogram<double> _operationDuration;

    private long _activeOperations;
    private long _awaitingConfirmation;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _ordersCreated = meter.CreateCounter<long>("orders.created", unit: "{order}",
            description: "Number of created orders");
        _ordersConfirmed = meter.CreateCounter<long>("orders.confirmed", unit: "{order}",
            description: "Number of orders with confirmed stock");
        _ordersStatusChanged = meter.CreateCounter<long>("orders.status_changed", unit: "{order}",
            description: "Number of order status transitions");
        _ordersDeleted = meter.CreateCounter<long>("orders.deleted", unit: "{order}",
            description: "Number of deleted orders");
        _operationDuration = meter.CreateHistogram<double>("orders.operation.duration", unit: "ms",
            description: "Duration of order operations, successful and failed");

        meter.CreateObservableGauge("orders.awaiting_confirmation",
            () => Interlocked.Read(ref _awaitingConfirmation),
            unit: "{order}", description: "Submitted orders waiting for stock confirmation");
        meter.CreateObservableGauge("orders.operations.active",
            () => Interlocked.Read(ref _activeOperations),
            unit: "{operation}", description: "Order operations being processed right now");
    }

    public void OrderCreated(string status)
        => _ordersCreated.Add(1, new KeyValuePair<string, object?>("order.status", status));

    public void OrderConfirmed()
        => _ordersConfirmed.Add(1);

    public void StatusChanged(string newStatus)
        => _ordersStatusChanged.Add(1, new KeyValuePair<string, object?>("order.status", newStatus));

    public void OrderDeleted()
        => _ordersDeleted.Add(1);

    public void SetAwaitingConfirmation(long count)
        => Interlocked.Exchange(ref _awaitingConfirmation, count);

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
