namespace Orders.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
