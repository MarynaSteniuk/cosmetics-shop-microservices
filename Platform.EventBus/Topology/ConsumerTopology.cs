namespace Platform.EventBus.Topology;

public sealed record ConsumerTopology(string Exchange, string Queue, IReadOnlyList<string> RoutingKeys)
{
    public string DeadLetterExchange => EventBusNames.DeadLetterExchange(Exchange);
    public string DeadLetterQueue => EventBusNames.DeadLetterQueue(Queue);
}
