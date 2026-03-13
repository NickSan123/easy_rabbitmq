using Microsoft.Extensions.Hosting;

namespace easy_rabbitmq.Consumer;

public class RabbitMQConsumerHostedService(
    RabbitMQConsumerStarter starter)
    : IHostedService
{
    private readonly RabbitMQConsumerStarter _starter = starter;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _starter.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}