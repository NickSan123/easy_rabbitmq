using easy_rabbitmq.Enums;

namespace easy_rabbitmq.Configuration;

public class RabbitMQTopology
{
    public string Exchange { get; set; } = default!;
    public RabbitMQExchangeType ExchangeType { get; set; } = RabbitMQExchangeType.Topic;
    public string Queue { get; set; } = default!;
    public string RoutingKey { get; set; } = "#";
    public bool Durable { get; set; } = true;
}
