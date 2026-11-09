using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Platform.EventBus.Serialization;
using Platform.EventBus.Topology;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Consuming;

public abstract class RabbitMqConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly EventHandlerRegistry _handlers = new();

    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    protected RabbitMqConsumer(IConnection connection, IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected abstract ConsumerTopology Topology { get; }

    protected virtual int MaxRetries => 3;

    protected virtual ushort PrefetchCount => 10;

    protected abstract void ConfigureHandlers(EventHandlerRegistry handlers);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        ConfigureHandlers(_handlers);

        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await TopologyDeclarer.DeclareConsumerAsync(_channel, Topology, _logger, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageAsync;

        await _channel.BasicConsumeAsync(Topology.Queue, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("Consumer started on queue {Queue}, prefetch {PrefetchCount}, max retries {MaxRetries}",
            Topology.Queue, PrefetchCount, MaxRetries);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer on queue {Queue} is stopping", Topology.Queue);
        }
        finally
        {
            await _channel.CloseAsync(CancellationToken.None);
            await _channel.DisposeAsync();
        }
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        var properties = ea.BasicProperties;
        var body = ea.Body.ToArray();
        var eventType = properties.Type ?? ReadHeader(properties, EventBusNames.Headers.EventType);
        var correlationId = ReadHeader(properties, EventBusNames.Headers.CorrelationId) ?? properties.CorrelationId;
        var retryCount = ReadInt(properties, EventBusNames.Headers.RetryCount);

        using var activity = TracePropagation.StartConsumerActivity(ea, Topology.Queue);
        activity?.SetTag("messaging.rabbitmq.retry_count", retryCount);
        activity?.SetTag("correlation.id", correlationId);

        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventId"] = properties.MessageId,
            ["EventType"] = eventType,
            ["CorrelationId"] = correlationId,
            ["Queue"] = Topology.Queue,
            ["RetryCount"] = retryCount
        });

        try
        {
            if (!_handlers.TryGet(eventType, out var registration))
                throw new PermanentEventFailureException($"No handler for event type '{eventType ?? "<empty>"}'");

            var schemaVersion = ReadInt(properties, EventBusNames.Headers.SchemaVersion);
            if (schemaVersion > IntegrationEvent.CurrentSchemaVersion)
                throw new PermanentEventFailureException($"Unsupported schema version {schemaVersion}");

            IntegrationEvent @event;
            try
            {
                @event = JsonSerializer.Deserialize(body, registration.EventType, EventJson.Options) as IntegrationEvent
                         ?? throw new JsonException("Empty message body");
            }
            catch (JsonException ex)
            {
                throw new PermanentEventFailureException("Malformed JSON message", ex);
            }

            _logger.LogInformation("Received {EventType} {EventId} from {Queue}, attempt {Attempt}",
                eventType, @event.EventId, Topology.Queue, retryCount + 1);

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await registration.Handle(scope.ServiceProvider, @event, _stoppingToken);
            }

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            _logger.LogInformation("Processed {EventType} {EventId} from {Queue}: ack", eventType, @event.EventId,
                Topology.Queue);
        }
        catch (PermanentEventFailureException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex,
                "Permanent failure of {EventType} {MessageId} on {Queue}: {Reason}. Nack without requeue, message goes to {DeadLetterQueue}",
                eventType, properties.MessageId, Topology.Queue, ex.Message, Topology.DeadLetterQueue);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("{EventType} {MessageId} interrupted by shutdown, returned to {Queue}",
                eventType, properties.MessageId, Topology.Queue);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await RetryOrDeadLetterAsync(ea, properties, body, eventType, retryCount, ex);
        }
    }

    private async Task RetryOrDeadLetterAsync(BasicDeliverEventArgs ea, IReadOnlyBasicProperties properties,
        byte[] body, string? eventType, int retryCount, Exception ex)
    {
        if (retryCount >= MaxRetries)
        {
            _logger.LogError(ex,
                "{EventType} {MessageId} failed after {Attempts} attempts on {Queue}, sending to {DeadLetterQueue}",
                eventType, properties.MessageId, retryCount + 1, Topology.Queue, Topology.DeadLetterQueue);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
        _logger.LogWarning(ex,
            "{EventType} {MessageId} failed on {Queue}, retry {Attempt}/{MaxRetries} in {DelayMs} ms: {Reason}",
            eventType, properties.MessageId, Topology.Queue, retryCount + 1, MaxRetries, delay.TotalMilliseconds,
            ex.Message);

        try
        {
            await Task.Delay(delay, _stoppingToken);
        }
        catch (OperationCanceledException)
        {
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            return;
        }

        var headers = new Dictionary<string, object?>(properties.Headers ?? new Dictionary<string, object?>())
        {
            [EventBusNames.Headers.RetryCount] = retryCount + 1,
            [EventBusNames.Headers.LastError] = ex.GetType().Name
        };
        var retryProperties = new BasicProperties(properties) { Headers = headers };

        await _channel!.BasicPublishAsync(exchange: string.Empty, routingKey: Topology.Queue, mandatory: false,
            retryProperties, body);
        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }

    private static string? ReadHeader(IReadOnlyBasicProperties properties, string key)
        => properties.Headers is not null && properties.Headers.TryGetValue(key, out var value)
            ? TracePropagation.ReadString(value)
            : null;

    private static int ReadInt(IReadOnlyBasicProperties properties, string key)
    {
        if (properties.Headers is null || !properties.Headers.TryGetValue(key, out var value) || value is null)
            return 0;

        return value switch
        {
            int number => number,
            long number => (int)number,
            byte number => number,
            short number => number,
            _ => int.TryParse(TracePropagation.ReadString(value), out var parsed) ? parsed : 0
        };
    }
}
