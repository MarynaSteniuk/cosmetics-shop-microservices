using Catalog.Bll.Services;

namespace Catalog.Api.Metrics;

public sealed class CatalogMetricsSnapshotService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CatalogMetrics _metrics;
    private readonly ILogger<CatalogMetricsSnapshotService> _logger;

    public CatalogMetricsSnapshotService(IServiceScopeFactory scopeFactory, CatalogMetrics metrics,
        ILogger<CatalogMetricsSnapshotService> logger)
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
                var products = scope.ServiceProvider.GetRequiredService<IProductService>();

                var lowStock = await products.GetLowStockAsync(CatalogMetrics.LowStockThreshold, stoppingToken);
                _metrics.SetLowStockProducts(lowStock.Count);
                _logger.LogDebug("Low stock products: {LowStockCount}", lowStock.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to refresh catalog metrics snapshot");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
