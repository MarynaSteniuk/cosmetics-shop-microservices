namespace Orders.Domain.Exceptions;

public sealed class BusinessConflictException : DomainException
{
    public BusinessConflictException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
