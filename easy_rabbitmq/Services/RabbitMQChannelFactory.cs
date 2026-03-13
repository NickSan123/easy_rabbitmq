using easy_rabbitmq.Abstractions;
using RabbitMQ.Client;
using System.Threading.Channels;

namespace easy_rabbitmq.Services;

public class RabbitMQChannelFactory : IRabbitMQChannelFactory
{
    private readonly IRabbitMQConnection _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMQChannelFactory(IRabbitMQConnection connection)
    {
        _connection = connection;
    }

    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_channel != null && _channel.IsOpen)
            return _channel;

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_channel != null && _channel.IsOpen)
                return _channel;

            var conn = await _connection.GetConnectionAsync(cancellationToken);

            _channel = await conn.CreateChannelAsync(cancellationToken: cancellationToken);

            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }
}