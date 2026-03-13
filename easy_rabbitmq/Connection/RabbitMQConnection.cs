using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace easy_rabbitmq.Connection;

public class RabbitMQConnection : IRabbitMQConnection, IAsyncDisposable
{
    private readonly IOptionsMonitor<RabbitMQOptions> _options;
    private readonly ILogger<RabbitMQConnection> _logger;
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMQConnection(IOptionsMonitor<RabbitMQOptions> options, ILogger<RabbitMQConnection> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection != null && _connection.IsOpen)
            return _connection;

        await _lock.WaitAsync(ct);

        try
        {
            if (_connection != null && _connection.IsOpen)
                return _connection;

            var cfg = _options.CurrentValue;

            var factory = new ConnectionFactory
            {
                HostName = cfg.HostName,
                Port = cfg.Port,
                UserName = cfg.UserName,
                Password = cfg.Password,
                VirtualHost = cfg.VirtualHost,
                RequestedHeartbeat = TimeSpan.FromSeconds(cfg.RequestedHeartbeat),
                AutomaticRecoveryEnabled = cfg.AutomaticRecoveryEnabled,
                ClientProvidedName = cfg.ClientProvidedName,
                RequestedConnectionTimeout = TimeSpan.FromMilliseconds(cfg.RequestedConnectionTimeout),
            };

            try
            {
                _connection = await factory.CreateConnectionAsync();
                _logger.LogInformation("Conexão RabbitMQ criada em {host}:{port} (vhost={vhost})", cfg.HostName, cfg.Port, cfg.VirtualHost);
                return _connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao criar conexão RabbitMQ para {host}:{port}", cfg.HostName, cfg.Port);
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.CloseAsync();
                _logger.LogInformation("Conexão RabbitMQ encerrada");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao fechar conexão RabbitMQ");
            }

            _connection.Dispose();
            _connection = null;
        }

        _lock.Dispose();
    }
}
