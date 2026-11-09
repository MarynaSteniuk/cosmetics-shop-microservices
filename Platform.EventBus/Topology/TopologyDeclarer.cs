using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus.Topology;

public static class TopologyDeclarer
{
    public static Task DeclareExchangeAsync(IChannel channel, string exchange, CancellationToken ct)
        => channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false,
            cancellationToken: ct);

    public static async Task DeclareConsumerAsync(IChannel channel, ConsumerTopology topology, ILogger logger,
        CancellationToken ct)
    {
        await DeclareExchangeAsync(channel, topology.Exchange, ct);

        await channel.ExchangeDeclareAsync(topology.DeadLetterExchange, ExchangeType.Direct, durable: true,
            autoDelete: false, cancellationToken: ct);

        await channel.QueueDeclareAsync(topology.Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = topology.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = topology.Queue
            },
            cancellationToken: ct);

        foreach (var routingKey in topology.RoutingKeys)
            await channel.QueueBindAsync(topology.Queue, topology.Exchange, routingKey, cancellationToken: ct);

        await channel.QueueDeclareAsync(topology.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: ct);
        await channel.QueueBindAsync(topology.DeadLetterQueue, topology.DeadLetterExchange, topology.Queue,
            cancellationToken: ct);

        logger.LogInformation(
            "RabbitMQ topology declared: exchange {Exchange}, queue {Queue} bound to {RoutingKeys}, DLX {DeadLetterExchange}, DLQ {DeadLetterQueue}",
            topology.Exchange, topology.Queue, string.Join(", ", topology.RoutingKeys),
            topology.DeadLetterExchange, topology.DeadLetterQueue);
    }
}
