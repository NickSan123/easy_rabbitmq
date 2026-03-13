using easy_rabbitmq.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Threading.Channels;

namespace easy_rabbitmq.Services;

public class RabbitMQChannelFactory : IRabbitMQChannelFactory
{
    private readonly IRabbitMQConnection _connection;
    private readonly ILogger<RabbitMQChannelFactory> _logger;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMQChannelFactory(IRabbitMQConnection connection, ILogger<RabbitMQChannelFactory> logger)
    {
        _connection = connection;
        _logger = logger;
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

            // Try to enable publisher confirms on underlying model if possible
            try
            {
                // tenta encontrar objeto subjacente que suporte ConfirmSelect (procura em campos/propriedades privados)
                var enabled = false;
                var t = _channel.GetType();

                foreach (var f in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
                {
                    var val = f.GetValue(_channel);
                    if (val == null) continue;
                    var m = val.GetType().GetMethod("ConfirmSelect");
                    if (m != null)
                    {
                        m.Invoke(val, null);
                        _logger.LogInformation("ConfirmSelect invocado no objeto subjacente (campo: {field})", f.Name);
                        enabled = true;
                        break;
                    }
                }

                if (!enabled)
                {
                    foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
                    {
                        if (!p.CanRead) continue;
                        var val = p.GetValue(_channel);
                        if (val == null) continue;
                        var m = val.GetType().GetMethod("ConfirmSelect");
                        if (m != null)
                        {
                            m.Invoke(val, null);
                            _logger.LogInformation("ConfirmSelect invocado no objeto subjacente (propriedade: {prop})", p.Name);
                            enabled = true;
                            break;
                        }
                    }
                }

                if (!enabled)
                {
                    _logger.LogInformation("Canal criado (método ConfirmSelect não encontrado no canal nem em objetos subjacentes)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Falha ao invocar ConfirmSelect no canal/sub-objeto");
            }

            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }
}
