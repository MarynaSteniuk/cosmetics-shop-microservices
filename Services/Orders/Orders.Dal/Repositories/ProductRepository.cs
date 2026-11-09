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

    public async Task<CatalogSyncResult> ApplyCatalogChangeAsync(long productId, string name, decimal? price,
        DateTime changedAt, CancellationToken ct = default)
    {
        const string update = """
            UPDATE dbo.Products SET
                Name = CASE WHEN CatalogNameSyncedAt IS NULL OR CatalogNameSyncedAt < @ChangedAt
                            THEN @Name ELSE Name END,
                CatalogNameSyncedAt = CASE WHEN CatalogNameSyncedAt IS NULL OR CatalogNameSyncedAt < @ChangedAt
                            THEN @ChangedAt ELSE CatalogNameSyncedAt END,
                Price = CASE WHEN @Price IS NOT NULL AND (CatalogPriceSyncedAt IS NULL OR CatalogPriceSyncedAt < @ChangedAt)
                            THEN @Price ELSE Price END,
                CatalogPriceSyncedAt = CASE WHEN @Price IS NOT NULL AND (CatalogPriceSyncedAt IS NULL OR CatalogPriceSyncedAt < @ChangedAt)
                            THEN @ChangedAt ELSE CatalogPriceSyncedAt END,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedBy = @Actor
            WHERE Id = @Id AND IsDeleted = 0
              AND (CatalogNameSyncedAt IS NULL OR CatalogNameSyncedAt < @ChangedAt
                   OR (@Price IS NOT NULL AND (CatalogPriceSyncedAt IS NULL OR CatalogPriceSyncedAt < @ChangedAt)))
            """;
        const string exists = "SELECT COUNT(*) FROM dbo.Products WHERE Id = @Id";
        const string insert = """
            INSERT INTO dbo.Products (Id, Name, Price, Stock, CreatedBy, CatalogNameSyncedAt, CatalogPriceSyncedAt)
            VALUES (@Id, @Name, @Price, 0, @Actor, @ChangedAt, @ChangedAt)
            """;

        var parameters = new { Id = productId, Name = name, Price = price, ChangedAt = changedAt, Actor = "catalog-sync" };

        await EnsureOpenAsync(ct);

        if (await Connection.ExecuteAsync(Command(update, parameters, ct)) > 0)
            return CatalogSyncResult.Updated;

        if (await Connection.ExecuteScalarAsync<int>(Command(exists, parameters, ct)) > 0)
            return CatalogSyncResult.Stale;

        if (price is null)
            return CatalogSyncResult.NotFound;

        await Connection.ExecuteAsync(Command(insert, parameters, ct));
        return CatalogSyncResult.Inserted;
    }
}
