namespace Orders.Domain.Exceptions;

public sealed class ValidationException : DomainException
{
    public ValidationException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
