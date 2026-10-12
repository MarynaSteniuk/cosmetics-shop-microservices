using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class ProductRepository : BaseDapperRepository<Product>, IProductRepository
{
    public ProductRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Products") { }

    public override async Task<long> AddAsync(Product product, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO dbo.Products (Id, Name, Price, Stock) VALUES (@Id, @Name, @Price, @Stock)";

        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(Command(sql,
                new { product.Id, product.Name, product.Price, product.Stock }, ct));
        }
        catch (SqlException ex) when (SqlExceptionMapper.Translate(ex) is { } mapped)
        {
            throw mapped;
        }
        return product.Id;
    }

    public override async Task<bool> UpdateAsync(Product product, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Products SET Name = @Name, Price = @Price, Stock = @Stock, " +
                           "UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        try
        {
            var affected = await Connection.ExecuteAsync(Command(sql,
                new { product.Id, product.Name, product.Price, product.Stock }, ct));
            return affected > 0;
        }
        catch (SqlException ex) when (SqlExceptionMapper.Translate(ex) is { } mapped)
        {
            throw mapped;
        }
    }

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Name, Price, Stock FROM dbo.Products WHERE Id IN @Ids AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Product>(Command(sql, new { Ids = ids.ToArray() }, ct));
        return rows.AsList();
    }

    public async Task DecreaseStockAsync(long productId, int units, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Products SET Stock = Stock - @Units, UpdatedAt = SYSUTCDATETIME() " +
                           "WHERE Id = @Id AND IsDeleted = 0 AND Stock >= @Units";

        await EnsureOpenAsync(ct);
        var affected = await Connection.ExecuteAsync(Command(sql, new { Id = productId, Units = units }, ct));

        if (affected == 0)
            throw new BusinessConflictException($"Not enough stock for product {productId}");
    }
}
