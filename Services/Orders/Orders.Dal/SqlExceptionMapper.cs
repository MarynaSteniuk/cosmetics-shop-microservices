using Microsoft.Data.SqlClient;
using Orders.Domain.Exceptions;

namespace Orders.Dal;

internal static class SqlExceptionMapper
{
    public static Exception? Translate(SqlException ex) => ex.Number switch
    {
        50001 or 50020 or 50032 => new NotFoundException(ex.Message, ex),
        50002 or 50003 or 50004 or 50010 or 50011 or 50031 or 50040 => new BusinessConflictException(ex.Message, ex),
        50021 or 50030 => new ValidationException(ex.Message, ex),
        2601 or 2627 => new BusinessConflictException("Record with the same key already exists", ex),
        547 => new BusinessConflictException("The operation violates a database constraint", ex),
        _ => null
    };
}
