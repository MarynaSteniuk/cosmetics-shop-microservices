using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Dal.Interfaces;

namespace Orders.Dal.Repositories;

public sealed class ProcessedEventRepository : RepositoryBase, IProcessedEventRepository
{
    private const int PrimaryKeyViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public ProcessedEventRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction) { }

    public async Task<bool> TryAddAsync(Guid eventId, string eventType, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO dbo.ProcessedEvents (EventId, EventType) VALUES (@EventId, @EventType)";

        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(sql, new { EventId = eventId, EventType = eventType },
                Transaction, CommandTimeoutSeconds, cancellationToken: ct));
            return true;
        }
        catch (SqlException ex) when (ex.Number is PrimaryKeyViolation or UniqueIndexViolation)
        {
            return false;
        }
    }
}
