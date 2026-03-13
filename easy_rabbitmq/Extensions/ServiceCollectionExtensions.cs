using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Channel;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Consumer;
using easy_rabbitmq.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace easy_rabbitmq.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEasyRabbitMQ(
        this IServiceCollection services,
        Action<RabbitMQOptions> configure)
    {
        // Configuração via Options Pattern
        services.Configure(configure);

        services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
        services.AddSingleton<IRabbitMQChannelFactory, RabbitMQChannelFactory>();

        services.AddSingleton<IRabbitMQChannelPool, RabbitMQChannelPool>();

        services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();

        services.AddSingleton<RabbitMQConsumerStarter>();
        services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();

        // Auto start dos consumers
        services.AddHostedService<RabbitMQConsumerHostedService>();

        return services;
    }

    /// <summary>
    /// Registra automaticamente todos os consumers de um assembly
    /// </summary>
    public static IServiceCollection AddRabbitMQConsumersFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var consumers = RabbitMQConsumerScanner.GetConsumers(assembly);

        foreach (var consumer in consumers)
        {
            services.AddScoped(consumer);
        }

        return services;
    }
}