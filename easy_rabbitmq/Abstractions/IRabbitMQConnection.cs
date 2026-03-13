using RabbitMQ.Client;

namespace easy_rabbitmq.Abstractions;

public interface IRabbitMQConnection
{
    Task<IConnection> GetConnectionAsync(CancellationToken ct = default);
}