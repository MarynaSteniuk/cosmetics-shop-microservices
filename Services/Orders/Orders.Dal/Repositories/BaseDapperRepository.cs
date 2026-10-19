using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;

namespace Orders.Dal.Repositories;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    protected readonly string TableName;

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction, string tableName)
        : base(connection, currentTransaction)
    {
        TableName = tableName;
    }

    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<T>(
            Command($"SELECT * FROM {TableName} WHERE Id = @Id AND IsDeleted = 0", new { Id = id }, ct));
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<T>(
            Command($"SELECT * FROM {TableName} WHERE IsDeleted = 0 ORDER BY Id", null, ct));
        return rows.AsList();
    }

    public virtual async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var affected = await Connection.ExecuteAsync(Command(
            $"UPDATE {TableName} SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id }, ct));
        return affected > 0;
    }

    public abstract Task<long> AddAsync(T entity, CancellationToken ct = default);
    public abstract Task<bool> UpdateAsync(T entity, CancellationToken ct = default);

    protected CommandDefinition Command(string sql, object? parameters, CancellationToken ct,
        CommandType commandType = CommandType.Text)
        => new(sql, parameters, Transaction, CommandTimeoutSeconds, commandType, cancellationToken: ct);
}
