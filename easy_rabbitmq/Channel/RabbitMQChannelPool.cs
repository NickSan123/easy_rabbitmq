using easy_rabbitmq.Abstractions;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace easy_rabbitmq.Channel;

public class RabbitMQChannelPool(IRabbitMQConnection connection, int maxChannels = 20) : IRabbitMQChannelPool
{
    private readonly IRabbitMQConnection _connection = connection;
    private readonly ConcurrentBag<IChannel> _channels = [];
    private readonly int _maxChannels = maxChannels;
    private int _createdChannels;

    public async Task<IChannel> RentAsync(CancellationToken cancellationToken = default)
    {
        if (_channels.TryTake(out var channel))
        {
            if (channel.IsOpen)
                return channel;

            await channel.DisposeAsync();
        }

        if (_createdChannels < _maxChannels)
        {
            var connection = await _connection.GetConnectionAsync(cancellationToken);

            var newChannel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            Interlocked.Increment(ref _createdChannels);

            return newChannel;
        }

        // aguarda até algum channel voltar
        while (true)
        {
            if (_channels.TryTake(out channel))
                return channel;

            await Task.Delay(10, cancellationToken);
        }
    }

    public void Return(IChannel channel)
    {
        if (!channel.IsOpen)
        {
            channel.Dispose();
            Interlocked.Decrement(ref _createdChannels);
            return;
        }

        _channels.Add(channel);
    }
}
