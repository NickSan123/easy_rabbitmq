namespace easy_rabbitmq.Abstractions;

public interface IRabbitMQMessageConsumer<T>
{
    Task HandleAsync(T message);
}