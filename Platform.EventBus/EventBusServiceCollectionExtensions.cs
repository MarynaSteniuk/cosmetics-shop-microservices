using Platform.EventBus.Consuming;
using Platform.EventBus.Publishing;

namespace Microsoft.Extensions.DependencyInjection;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddEventPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        return services;
    }

    public static IServiceCollection AddIntegrationEventConsumer<TConsumer>(this IServiceCollection services)
        where TConsumer : RabbitMqConsumer
    {
        services.AddHostedService<TConsumer>();
        return services;
    }
}
