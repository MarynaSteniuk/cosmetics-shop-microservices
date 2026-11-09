namespace Catalog.Bll.Events;

public interface IProductEventsPublisher
{
    Task PriceChangedAsync(long productId, string sku, string productName, decimal oldPrice, decimal newPrice,
        DateTime changedAt, CancellationToken ct);

    Task RenamedAsync(long productId, string sku, string oldName, string newName, DateTime changedAt,
        CancellationToken ct);
}
