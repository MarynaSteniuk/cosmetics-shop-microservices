using Orders.Bll.Services;
using Orders.Domain;

namespace Orders.Api.Metrics;

public sealed class OrdersMetricsSnapshotService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderMetrics _metrics;
    private readonly ILogger<OrdersMetricsSnapshotService> _logger;

    public OrdersMetricsSnapshotService(IServiceScopeFactory scopeFactory, OrderMetrics metrics,
        ILogger<OrdersMetricsSnapshotService> logger)
    {
        _scopeFactory = scopeFactory;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();

                var count = await orders.CountByStatusAsync(OrderStatuses.Submitted, stoppingToken);
                _metrics.SetAwaitingConfirmation(count);
                _logger.LogDebug("Orders awaiting confirmation: {AwaitingCount}", count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to refresh order metrics snapshot");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
