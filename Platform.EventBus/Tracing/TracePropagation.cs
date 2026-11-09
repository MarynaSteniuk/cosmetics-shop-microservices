using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Tracing;

public static class TracePropagation
{
    public const string SourceName = "Platform.Messaging";

    public static readonly ActivitySource Source = new(SourceName);

    private static TextMapPropagator Propagator => Propagators.DefaultTextMapPropagator;

    public static Activity? StartPublishActivity(string exchange, string routingKey, string eventType, Guid eventId)
    {
        var activity = Source.StartActivity($"{exchange} publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "publish");
        activity?.SetTag("messaging.destination.name", exchange);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", routingKey);
        activity?.SetTag("messaging.message.id", eventId.ToString());
        activity?.SetTag("messaging.event.type", eventType);
        return activity;
    }

    public static void Inject(BasicProperties properties)
    {
        if (Activity.Current is not { } activity)
            return;

        properties.Headers ??= new Dictionary<string, object?>();
        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), properties.Headers,
            static (headers, key, value) => headers[key] = value);
    }

    public static Activity? StartConsumerActivity(BasicDeliverEventArgs ea, string queue)
    {
        var parent = Propagator.Extract(default, ea.BasicProperties.Headers,
            static (headers, key) => headers is not null && headers.TryGetValue(key, out var value)
                ? [ReadString(value) ?? string.Empty]
                : []);

        Baggage.Current = parent.Baggage;

        var activity = Source.StartActivity($"{queue} process", ActivityKind.Consumer, parent.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.destination.name", queue);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", ea.RoutingKey);
        activity?.SetTag("messaging.message.id", ea.BasicProperties.MessageId);
        activity?.SetTag("messaging.event.type", ea.BasicProperties.Type);
        return activity;
    }

    public static string? ReadString(object? value) => value switch
    {
        null => null,
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        string text => text,
        _ => value.ToString()
    };
}
