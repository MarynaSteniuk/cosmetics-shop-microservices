namespace Platform.EventBus.Events;

public abstract record IntegrationEvent
{
    public const int CurrentSchemaVersion = 1;

    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
}
