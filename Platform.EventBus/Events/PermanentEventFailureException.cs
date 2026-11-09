namespace Platform.EventBus.Events;

public sealed class PermanentEventFailureException : Exception
{
    public PermanentEventFailureException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
