using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Channel;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Consumer;
using easy_rabbitmq.Connection;
using easy_rabbitmq.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using easy_rabbitmq.Topology;

namespace easy_rabbitmq.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEasyRabbitMQ(
        this IServiceCollection services,
        Action<RabbitMQOptions> configure, RabbitMQTopology topology)
    {
        // Configuração via Options Pattern
        services.Configure(configure);

        // Topology manager to coordinate readiness
        services.AddSingleton<TopologyManager>();

        services.AddSingleton<IRabbitMQConnection, RabbitMQConnection>();
        services.AddSingleton<IRabbitMQChannelFactory, RabbitMQChannelFactory>();

        services.AddSingleton<IRabbitMQChannelPool, RabbitMQChannelPool>();

        services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();

        services.AddSingleton<RabbitMQConsumerStarter>();
        services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();

        services.AddSingleton(topology);
        // Auto start dos consumers

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