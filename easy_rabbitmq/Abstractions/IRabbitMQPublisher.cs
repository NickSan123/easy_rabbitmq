namespace easy_rabbitmq.Abstractions;

public interface IRabbitMQPublisher
{
    Task PublishAsync<T>(
        string exchange,
        string routingKey,
        T message,
        CancellationToken cancellationToken = default);
}


