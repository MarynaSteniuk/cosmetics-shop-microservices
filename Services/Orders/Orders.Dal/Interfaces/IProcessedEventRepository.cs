namespace Orders.Dal.Interfaces;

public interface IProcessedEventRepository
{
    Task<bool> TryAddAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
