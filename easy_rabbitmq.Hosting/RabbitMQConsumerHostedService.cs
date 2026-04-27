using easy_rabbitmq.Configuration;
using easy_rabbitmq.Consumer;
using Microsoft.Extensions.Hosting;

namespace easy_rabbitmq.Hosting;

public class RabbitMQConsumerHostedService(
    RabbitMQConsumerStarter starter, RabbitMQTopology topology)
    : IHostedService
{
    private readonly RabbitMQConsumerStarter _starter = starter;
    private readonly RabbitMQTopology _topology = topology;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _starter.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}