using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(long customerId, string? status, CancellationToken ct = default);
    Task<PromoCode?> FindActivePromoCodeAsync(string code, CancellationToken ct = default);
    Task AddDetailsAsync(OrderDetails details, CancellationToken ct = default);
    Task AddItemAsync(OrderItem item, CancellationToken ct = default);
    Task AddStatusHistoryAsync(long orderId, string? oldStatus, string newStatus, string? changedBy, CancellationToken ct = default);
    Task ChangeStatusAsync(long orderId, string newStatus, string? changedBy, CancellationToken ct = default);
    Task ConfirmAsync(long orderId, string? changedBy, CancellationToken ct = default);
}
