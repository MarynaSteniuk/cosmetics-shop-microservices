namespace Platform.Aggregator.Services;

public sealed class DependencyUnavailableException : Exception
{
    public DependencyUnavailableException(string service, Exception inner)
        : base($"{service} is unavailable", inner)
    {
        Service = service;
    }

    public string Service { get; }
}
