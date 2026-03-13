using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Channel;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Consumer;
using easy_rabbitmq.Services;
using Microsoft.Extensions.DependencyInjection;

namespace easy_rabbitmq.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEasyRabbitMQ(
        this IServiceCollection services,
        Action<RabbitMQOptions> configure)
    {
        var options = new RabbitMQOptions();
        configure(options);

        services.AddSingleton(options);

        services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
        services.AddSingleton<IRabbitMQChannelFactory, RabbitMQChannelFactory>();

        services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();

        services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();
        services.AddSingleton<IRabbitMQChannelPool, RabbitMQChannelPool>();

        return services;
    }
}
