using easy_rabbitmq.Configuration;
using RabbitMQ.Client;

namespace easy_rabbitmq.Topology;

public static class RabbitMQTopologyBuilder
{
    public static async Task DeclareAsync(
        IChannel channel,
        RabbitMQTopology topology)
    {
        await channel.ExchangeDeclareAsync(
            exchange: topology.Exchange,
            type: topology.ExchangeType.ToString().ToLower(),
            durable: topology.Durable);

        await channel.QueueDeclareAsync(
            queue: topology.Queue,
            durable: topology.Durable,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: topology.Queue,
            exchange: topology.Exchange,
            routingKey: topology.RoutingKey);
    }
    public static async Task DeclareWithRetryAsync(
    IChannel channel,
    RabbitMQTopology topology)
    {
        var args = new Dictionary<string, object>
    {
        { "x-dead-letter-exchange", $"{topology.Exchange}.dlx" }
    };

        await channel.ExchangeDeclareAsync(
            topology.Exchange,
            topology.ExchangeType.ToString().ToLower(),
            durable: true);

        await channel.ExchangeDeclareAsync(
            $"{topology.Exchange}.dlx",
            "fanout",
            durable: true);

        await channel.QueueDeclareAsync(
            topology.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args);

        await channel.QueueDeclareAsync(
            $"{topology.Queue}.dead",
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            topology.Queue,
            topology.Exchange,
            topology.RoutingKey);

        await channel.QueueBindAsync(
            $"{topology.Queue}.dead",
            $"{topology.Exchange}.dlx",
            "");
    }
}
