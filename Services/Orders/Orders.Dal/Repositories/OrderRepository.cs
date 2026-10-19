using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class OrderRepository : BaseDapperRepository<Order>, IOrderRepository
{
    public OrderRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Orders") { }

    public override async Task<long> AddAsync(Order order, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.Orders (CustomerId, PromoCodeId, Status, TotalAmount, CreatedBy)
OUTPUT INSERTED.Id
VALUES (@CustomerId, @PromoCodeId, @Status, @TotalAmount, @CreatedBy)";

        await EnsureOpenAsync(ct);
        return await Connection.ExecuteScalarAsync<long>(Command(sql,
            new { order.CustomerId, order.PromoCodeId, order.Status, order.TotalAmount, order.CreatedBy }, ct));
    }

    public override async Task<bool> UpdateAsync(Order order, CancellationToken ct = default)
    {
        const string sql = @"
UPDATE dbo.Orders
SET TotalAmount = @TotalAmount, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy
WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        var affected = await Connection.ExecuteAsync(Command(sql,
            new { order.Id, order.TotalAmount, order.UpdatedBy }, ct));
        return affected > 0;
    }

    public override async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(Command("dbo.usp_SoftDeleteOrder",
                new { OrderId = id }, ct, CommandType.StoredProcedure));
            return true;
        }
        catch (SqlException ex) when (SqlExceptionMapper.Translate(ex) is { } mapped)
        {
            throw mapped;
        }
    }

    public async Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default)
    {
        const string sql = @"
SELECT o.Id, o.CustomerId, o.PromoCodeId, o.Status, o.OrderDate, o.TotalAmount,
       o.CreatedAt, o.CreatedBy, o.UpdatedAt, o.UpdatedBy, o.RowVersion,
       d.ShippingAddress, d.City, d.Comment,
       i.ProductId, i.OrderId, i.ProductName, i.UnitPrice, i.Discount, i.Units
FROM dbo.Orders o
JOIN dbo.OrderDetails d ON d.OrderId = o.Id
LEFT JOIN dbo.OrderItems i ON i.OrderId = o.Id
WHERE o.Id = @OrderId AND o.IsDeleted = 0
ORDER BY i.ProductName";

        await EnsureOpenAsync(ct);
        Order? result = null;

        await Connection.QueryAsync<Order, OrderDetails, OrderItem?, Order>(
            Command(sql, new { OrderId = orderId }, ct),
            (order, details, item) =>
            {
                if (result is null)
                {
                    result = order;
                    details.OrderId = order.Id;
                    result.Details = details;
                }
                if (item is not null)
                    result.Items.Add(item);
                return result;
            },
            splitOn: "ShippingAddress,ProductId");

        return result;
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(long customerId, string? status, CancellationToken ct = default)
    {
        const string sql = @"
SELECT Id, CustomerId, PromoCodeId, Status, OrderDate, TotalAmount, CreatedAt
FROM dbo.Orders
WHERE CustomerId = @CustomerId AND IsDeleted = 0
  AND (@Status IS NULL OR Status = @Status)
ORDER BY OrderDate DESC";

        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Order>(Command(sql,
            new { CustomerId = customerId, Status = status }, ct));
        return rows.AsList();
    }

    public async Task<PromoCode?> FindActivePromoCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = @"
SELECT Id, Code, DiscountPercent, ValidTo, IsActive
FROM dbo.PromoCodes
WHERE Code = @Code AND IsActive = 1 AND (ValidTo IS NULL OR ValidTo >= SYSUTCDATETIME())";

        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<PromoCode>(Command(sql, new { Code = code }, ct));
    }

    public async Task AddDetailsAsync(OrderDetails details, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, City, Comment)
VALUES (@OrderId, @ShippingAddress, @City, @Comment)";

        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(Command(sql,
            new { details.OrderId, details.ShippingAddress, details.City, details.Comment }, ct));
    }

    public async Task AddItemAsync(OrderItem item, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Discount, Units)
VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Discount, @Units)";

        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(Command(sql,
                new { item.OrderId, item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.Units }, ct));
        }
        catch (SqlException ex) when (SqlExceptionMapper.Translate(ex) is { } mapped)
        {
            throw mapped;
        }
    }

    public async Task AddStatusHistoryAsync(long orderId, string? oldStatus, string newStatus, string? changedBy,
        CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy)
VALUES (@OrderId, @OldStatus, @NewStatus, @ChangedBy)";

        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(Command(sql,
            new { OrderId = orderId, OldStatus = oldStatus, NewStatus = newStatus, ChangedBy = changedBy }, ct));
    }

    public async Task ChangeStatusAsync(long orderId, string newStatus, string? changedBy, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(Command("dbo.usp_ChangeOrderStatus",
                new { OrderId = orderId, NewStatus = newStatus, ChangedBy = changedBy }, ct,
                CommandType.StoredProcedure));
        }
        catch (SqlException ex) when (SqlExceptionMapper.Translate(ex) is { } mapped)
        {
            throw mapped;
        }
    }

    public async Task ConfirmAsync(long orderId, string? changedBy, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(Command("dbo.usp_ConfirmOrder",
                new { OrderId = orderId, ChangedBy = changedBy }, ct,
                CommandType.StoredProcedure));
        }
        catch (SqlException ex) when (SqlExceptionMapper.Translate(ex) is { } mapped)
        {
            throw mapped;
        }
    }
}
