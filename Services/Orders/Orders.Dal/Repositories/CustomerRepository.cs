using System.Data;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Repositories;

public sealed class CustomerRepository : RepositoryBase, ICustomerRepository
{
    private const string SelectColumns =
        "Id, Email, FullName, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy";

    public CustomerRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction) { }

    public async Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT " + SelectColumns + " FROM dbo.Customers WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = CreateCommand(sql);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = "SELECT " + SelectColumns + " FROM dbo.Customers WHERE Email = @Email AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = CreateCommand(sql);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = email;

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Customer>> GetPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        const string sql = "SELECT " + SelectColumns + " FROM dbo.Customers WHERE IsDeleted = 0 " +
                           "ORDER BY Id OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        await EnsureOpenAsync(ct);
        await using var command = CreateCommand(sql);
        command.Parameters.Add("@Skip", SqlDbType.Int).Value = (page - 1) * pageSize;
        command.Parameters.Add("@Take", SqlDbType.Int).Value = pageSize;

        var result = new List<Customer>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(Map(reader));
        return result;
    }

    public async Task<long> AddAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO dbo.Customers (Email, FullName, Phone, CreatedBy) " +
                           "OUTPUT INSERTED.Id VALUES (@Email, @FullName, @Phone, @CreatedBy)";

        await EnsureOpenAsync(ct);
        await using var command = CreateCommand(sql);
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = customer.Email;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 200).Value = customer.FullName;
        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = (object?)customer.Phone ?? DBNull.Value;
        command.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100).Value = (object?)customer.CreatedBy ?? DBNull.Value;

        try
        {
            var id = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt64(id);
        }
        catch (SqlException ex) when (SqlExceptionMapper.Translate(ex) is { } mapped)
        {
            throw mapped;
        }
    }

    public async Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Customers SET Email = @Email, FullName = @FullName, Phone = @Phone, " +
                           "UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = CreateCommand(sql);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = customer.Id;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 256).Value = customer.Email;
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 200).Value = customer.FullName;
        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = (object?)customer.Phone ?? DBNull.Value;
        command.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 100).Value = (object?)customer.UpdatedBy ?? DBNull.Value;

        try
        {
            return await command.ExecuteNonQueryAsync(ct) > 0;
        }
        catch (SqlException ex) when (SqlExceptionMapper.Translate(ex) is { } mapped)
        {
            throw mapped;
        }
    }

    public async Task<bool> SoftDeleteAsync(long id, string? deletedBy, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.Customers SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME(), " +
                           "UpdatedBy = @UpdatedBy WHERE Id = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = CreateCommand(sql);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
        command.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 100).Value = (object?)deletedBy ?? DBNull.Value;

        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private SqlCommand CreateCommand(string sql) =>
        new(sql, Connection, Transaction) { CommandTimeout = CommandTimeoutSeconds };

    private static Customer Map(SqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Email = reader.GetString(1),
        FullName = reader.GetString(2),
        Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
        CreatedAt = reader.GetDateTime(4),
        CreatedBy = reader.IsDBNull(5) ? null : reader.GetString(5),
        UpdatedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
        UpdatedBy = reader.IsDBNull(7) ? null : reader.GetString(7)
    };
}
