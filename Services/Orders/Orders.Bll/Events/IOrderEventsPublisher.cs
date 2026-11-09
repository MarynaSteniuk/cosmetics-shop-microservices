namespace Orders.Bll.Events;

public sealed record OrderStockLine(long ProductId, int Units);

public interface IOrderEventsPublisher
{
    Task StockConfirmedAsync(long orderId, long customerId, IReadOnlyList<OrderStockLine> lines, DateTime confirmedAt,
        CancellationToken ct);
}
