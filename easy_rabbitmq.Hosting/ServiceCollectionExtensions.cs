using Microsoft.Extensions.DependencyInjection;

namespace easy_rabbitmq.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEasyRabbitMQHosting(this IServiceCollection services)
    {
        services.AddHostedService<RabbitMQConsumerHostedService>();
        return services;
    }
}