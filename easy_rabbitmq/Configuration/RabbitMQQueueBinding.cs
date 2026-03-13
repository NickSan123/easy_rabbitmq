namespace easy_rabbitmq.Configuration;

public class RabbitMQQueueBinding
{
    public string Queue { get; set; } = "";

    public string RoutingKey { get; set; } = "";

    public bool Durable { get; set; } = true;

    public Dictionary<string, object>? Arguments { get; set; }
}
