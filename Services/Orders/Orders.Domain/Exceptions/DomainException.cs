namespace Orders.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
