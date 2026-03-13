namespace easy_rabbitmq.Consumer;

[AttributeUsage(AttributeTargets.Class)]
public class RabbitMQConsumerAttribute(
    string exchange,
    string queue,
    string routingKey = "#") : Attribute
{
    public string Queue { get; } = queue;
    public string Exchange { get; } = exchange;
    public string RoutingKey { get; } = routingKey;
}
