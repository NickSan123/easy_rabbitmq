using easy_rabbitmq.Enums;

namespace easy_rabbitmq.Configuration;

public class RabbitMQTopology
{
    public string Exchange { get; set; } = default!;
    public RabbitMQExchangeType ExchangeType { get; set; } = RabbitMQExchangeType.Topic;
    public List<RabbitMQQueueTopology> Queues { get; set; } = [];
    public string RoutingKey { get; set; } = "#";
    public bool Durable { get; set; } = true;
    public RabbitMQRetryOptions? Retry { get; set; }
}
