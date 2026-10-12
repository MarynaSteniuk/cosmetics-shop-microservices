using Orders.Bll.Dtos;

namespace Orders.Bll.Services;

public interface IOrderService
{
    Task<OrderDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetByCustomerAsync(long customerId, string? status, CancellationToken ct = default);
    Task<OrderDto> CreateAsync(CreateOrderDto input, CancellationToken ct = default);
    Task ConfirmAsync(long id, CancellationToken ct = default);
    Task<OrderDto> ChangeStatusAsync(long id, ChangeOrderStatusDto input, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
