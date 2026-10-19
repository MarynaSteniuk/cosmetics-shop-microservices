using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetPageAsync(int page, int pageSize, CancellationToken ct = default);
    Task<long> AddAsync(Customer customer, CancellationToken ct = default);
    Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(long id, string? deletedBy, CancellationToken ct = default);
}
