using RabbitMQ.Client;

namespace easy_rabbitmq.Abstractions;

public interface IRabbitMQChannelFactory
{
    //Task<IChannel> CreateChannelAsync();
    Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
}
