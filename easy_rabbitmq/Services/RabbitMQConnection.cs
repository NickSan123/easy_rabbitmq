using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace easy_rabbitmq.Services;

public class RabbitMQConnection(RabbitMQOptions options) : IRabbitMQConnection
{
    private readonly RabbitMQOptions _options = options;
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection != null && _connection.IsOpen)
            return _connection;

        await _lock.WaitAsync(ct);

        try
        {
            if (_connection != null && _connection.IsOpen)
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                RequestedHeartbeat = TimeSpan.FromSeconds(_options.RequestedHeartbeat),
                AutomaticRecoveryEnabled = _options.AutomaticRecoveryEnabled,
                ClientProvidedName = _options.ClientProvidedName
            };

            _connection = await factory.CreateConnectionAsync();

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }
}