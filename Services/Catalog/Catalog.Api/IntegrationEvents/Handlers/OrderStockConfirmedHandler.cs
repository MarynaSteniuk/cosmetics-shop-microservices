using Catalog.Api.IntegrationEvents.Events;
using Catalog.Bll.Caching;
using Catalog.Dal;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Platform.EventBus.Events;

namespace Catalog.Api.IntegrationEvents.Handlers;

public sealed class OrderStockConfirmedHandler : IIntegrationEventHandler<OrderStockConfirmedEvent>
{
    private readonly CatalogDbContext _db;
    private readonly ITwoLevelCache _cache;
    private readonly ILogger<OrderStockConfirmedHandler> _logger;

    public OrderStockConfirmedHandler(CatalogDbContext db, ITwoLevelCache cache,
        ILogger<OrderStockConfirmedHandler> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task HandleAsync(OrderStockConfirmedEvent @event, CancellationToken ct)
    {
        if (@event.Items.Count == 0 || @event.Items.Any(i => i.ProductId <= 0 || i.Units <= 0))
            throw new PermanentEventFailureException(
                $"Invalid {nameof(OrderStockConfirmedEvent)} for order {@event.OrderId}: empty or invalid items");

        if (await _db.ProcessedEvents.AnyAsync(e => e.EventId == @event.EventId, ct))
        {
            _logger.LogInformation("Duplicate {EventType} {EventId} for order {OrderId} skipped",
                nameof(OrderStockConfirmedEvent), @event.EventId, @event.OrderId);
            return;
        }

        var productIds = @event.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.ProductCategories)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var item in @event.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new PermanentEventFailureException(
                    $"Product {item.ProductId} from order {@event.OrderId} does not exist in the catalog");

            try
            {
                product.RemoveStock(item.Units);
            }
            catch (DomainException ex)
            {
                throw new PermanentEventFailureException(
                    $"Order {@event.OrderId}: cannot remove {item.Units} units of product {item.ProductId}: {ex.Message}", ex);
            }
        }

        _db.ProcessedEvents.Add(new ProcessedEvent(@event.EventId, nameof(OrderStockConfirmedEvent)));

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            _logger.LogInformation("Duplicate {EventType} {EventId} for order {OrderId} skipped on save",
                nameof(OrderStockConfirmedEvent), @event.EventId, @event.OrderId);
            return;
        }

        await _cache.InvalidateAsync(
            products.Values.SelectMany(p => p.ProductCategories.Select(pc => CatalogCacheKeys.CategoryProducts(pc.CategoryId))
                .Prepend(CatalogCacheKeys.Product(p.Id))),
            "OrderStockConfirmed", ct);

        _logger.LogInformation(
            "Stock decreased for order {OrderId} from {EventType} {EventId}: {ItemsCount} items, products {ProductIds}",
            @event.OrderId, nameof(OrderStockConfirmedEvent), @event.EventId, @event.Items.Count,
            string.Join(", ", productIds));
    }
}
