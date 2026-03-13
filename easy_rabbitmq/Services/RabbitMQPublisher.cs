using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Serialization;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;


namespace easy_rabbitmq.Services;

public class RabbitMQPublisher : IRabbitMQPublisher
{
    private readonly IRabbitMQChannelPool _pool;

    public RabbitMQPublisher(IRabbitMQChannelPool pool) => _pool = pool;

    public async Task PublishAsync<T>(
        string exchange,
        string routingKey,
        T message, CancellationToken cancellationToken = default)
    {
        var channel = await _pool.RentAsync(cancellationToken);

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange,
                routingKey,
                body);
        }
        finally
        {
            _pool.Return(channel);
        }
    }
}