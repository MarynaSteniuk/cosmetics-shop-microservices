namespace Catalog.Domain.Entities;

public class ProcessedEvent
{
    private ProcessedEvent() { }

    public ProcessedEvent(Guid eventId, string eventType)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = DateTime.UtcNow;
    }

    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = "";
    public DateTime ProcessedAt { get; private set; }
}
