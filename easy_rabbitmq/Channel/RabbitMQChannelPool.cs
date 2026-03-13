using easy_rabbitmq.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Threading;

namespace easy_rabbitmq.Channel;

public class RabbitMQChannelPool : IRabbitMQChannelPool, IAsyncDisposable
{
    private readonly IRabbitMQConnection _connection;
    private readonly ConcurrentBag<IChannel> _channels = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxChannels;
    private int _createdChannels;
    private bool _disposed;
    private readonly ILogger<RabbitMQChannelPool> _logger;

    public RabbitMQChannelPool(IRabbitMQConnection connection, ILogger<RabbitMQChannelPool> logger, int maxChannels = 20)
    {
        _connection = connection;
        _logger = logger;
        _maxChannels = maxChannels;
        _semaphore = new SemaphoreSlim(maxChannels, maxChannels);
    }

    public async Task<IChannel> RentAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        if (_disposed)
        {
            _semaphore.Release();
            throw new ObjectDisposedException(nameof(RabbitMQChannelPool));
        }

        try
        {
            while (_channels.TryTake(out var channel))
            {
                if (channel.IsOpen)
                    return channel;

                try { await channel.DisposeAsync(); } catch { }
                Interlocked.Decrement(ref _createdChannels);
            }

            if (_createdChannels < _maxChannels)
            {
                var conn = await _connection.GetConnectionAsync(cancellationToken);

                // A partir do RabbitMQ.Client 7, publisher confirms são habilitados
                // via opções de criação de canal, e não mais por ConfirmSelect.
                var channelOptions = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true,
                    outstandingPublisherConfirmationsRateLimiter: null,
                    consumerDispatchConcurrency: 1);

                var newChannel = await conn.CreateChannelAsync(channelOptions, cancellationToken: cancellationToken);

                Interlocked.Increment(ref _createdChannels);
                _logger.LogInformation("Novo canal criado com publisher confirmations habilitado. Total criado: {count}", _createdChannels);
                return newChannel;
            }

            // fallback: wait until a channel is returned
            while (true)
            {
                if (_channels.TryTake(out var channel))
                {
                    if (channel.IsOpen)
                        return channel;

                    try { await channel.DisposeAsync(); } catch { }
                    Interlocked.Decrement(ref _createdChannels);
                }

                await Task.Delay(50, cancellationToken);
            }
        }
        finally
        {
            // keep semaphore slot reserved until Return is called
        }
    }

    public void Return(IChannel channel)
    {
        if (_disposed)
        {
            try { channel.Dispose(); } catch { }
            return;
        }

        if (!channel.IsOpen)
        {
            try { channel.Dispose(); } catch { }
            Interlocked.Decrement(ref _createdChannels);
            _semaphore.Release();
            return;
        }

        _channels.Add(channel);
        _semaphore.Release();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        while (_channels.TryTake(out var ch))
        {
            try { await ch.DisposeAsync(); } catch { }
        }

        _semaphore.Dispose();
    }
}
