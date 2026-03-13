using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Serialization;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;


namespace easy_rabbitmq.Services;

public class RabbitMQPublisher(IRabbitMQChannelPool pool, easy_rabbitmq.Topology.TopologyManager topologyManager, ILogger<RabbitMQPublisher> logger, easy_rabbitmq.Abstractions.IRabbitMQConnection connection) : IRabbitMQPublisher
{
    private readonly IRabbitMQChannelPool _pool = pool;

    private readonly easy_rabbitmq.Topology.TopologyManager _topologyManager = topologyManager;
    private readonly ILogger<RabbitMQPublisher> _logger = logger;
    private readonly easy_rabbitmq.Abstractions.IRabbitMQConnection _connection = connection;

    public async Task PublishAsync<T>(
        string exchange,
        string routingKey,
        T message, CancellationToken cancellationToken = default)
    {
        // espera até que a topologia esteja pronta (criada pelo starter)
        var ready = _topologyManager.Ready;
        if (!ready.IsCompleted)
        {
            // espera um curto timeout para ambientes onde a topologia é criada manualmente (ex: apps de producer standalone)
            var completed = await Task.WhenAny(ready, Task.Delay(TimeSpan.FromSeconds(1), cancellationToken));
            if (completed != ready)
            {
                _logger.LogWarning("Topologia ainda não sinalizada como pronta; prosseguindo após timeout curto.");
            }
            else
            {
                await ready.ConfigureAwait(false);
            }
        }

        var channel = await _pool.RentAsync(cancellationToken);

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            // Com o canal criado com publisher confirmations habilitado (via CreateChannelOptions),
            // o próprio BasicPublishAsync já aguarda o ACK/NACK do broker e lança exceção em caso de falha.
            await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken: cancellationToken);
        }
        finally
        {
            _pool.Return(channel);
        }
    }
}