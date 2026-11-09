using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Platform.EventBus.Serialization;
using Platform.EventBus.Topology;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;

namespace Platform.EventBus.Publishing;

public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(IConnection connection, ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string exchange, string routingKey,
        CancellationToken ct = default) where TEvent : IntegrationEvent
    {
        var eventType = typeof(TEvent).Name;
        using var activity = TracePropagation.StartPublishActivity(exchange, routingKey, eventType, @event.EventId);

        try
        {
            await using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await TopologyDeclarer.DeclareExchangeAsync(channel, exchange, ct);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                MessageId = @event.EventId.ToString(),
                CorrelationId = @event.CorrelationId,
                Type = eventType,
                Timestamp = new AmqpTimestamp(new DateTimeOffset(@event.OccurredAt, TimeSpan.Zero).ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>
                {
                    [EventBusNames.Headers.CorrelationId] = @event.CorrelationId,
                    [EventBusNames.Headers.EventType] = eventType,
                    [EventBusNames.Headers.SchemaVersion] = @event.SchemaVersion
                }
            };

            TracePropagation.Inject(properties);

            var body = JsonSerializer.SerializeToUtf8Bytes(@event, EventJson.Options);
            await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, properties, body, ct);

            _logger.LogInformation(
                "Published {EventType} {EventId} to {Exchange}/{RoutingKey}, CorrelationId {CorrelationId}, {SizeBytes} bytes",
                eventType, @event.EventId, exchange, routingKey, @event.CorrelationId, body.Length);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex,
                "Failed to publish {EventType} {EventId} to {Exchange}/{RoutingKey}, CorrelationId {CorrelationId}",
                eventType, @event.EventId, exchange, routingKey, @event.CorrelationId);
            throw;
        }
    }
}
