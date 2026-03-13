using RabbitMQ.Client;

namespace easy_rabbitmq.Abstractions;

public interface IRabbitMQChannelPool
{
    Task<IChannel> RentAsync(CancellationToken cancellationToken = default);

    void Return(IChannel channel);
}
