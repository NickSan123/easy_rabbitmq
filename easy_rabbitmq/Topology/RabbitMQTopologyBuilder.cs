using easy_rabbitmq.Configuration;
using easy_rabbitmq.Extensions;
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
            type: topology.ExchangeType.ToExchangeString(),
            durable: topology.Durable);

        foreach (var queue in topology.Queues)
        {
            if (topology.Retry?.Enabled == true &&
                topology.Retry.Delays.Length > 0)
            {
                await DeclareWithRetryAsync(
                    channel,
                    topology,
                    queue);

                continue;
            }

            await SafeQueueDeclareAsync(channel,
                queue: queue.Queue,
                durable: queue.Durable,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await channel.QueueBindAsync(
                queue: queue.Queue,
                exchange: topology.Exchange,
                routingKey: queue.RoutingKey);
        }
    }

    private static async Task DeclareWithRetryAsync(
        IChannel channel,
        RabbitMQTopology topology,
        RabbitMQQueueTopology queue)
    {
        var retry = topology.Retry!;
        var exchange = topology.Exchange;
        var mainRoutingKey = queue.RoutingKey;
        var deadRoutingKey = $"{mainRoutingKey}.dead";

        // garante que exista apenas o exchange principal
        await channel.ExchangeDeclareAsync(
            exchange,
            topology.ExchangeType.ToExchangeString(),
            durable: true);

        // main queue deve enviar para o primeiro retry via dead-letter (quando consumer rejeitar)
        var mainArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", exchange },
            { "x-dead-letter-routing-key", $"{mainRoutingKey}.retry.1" }
        };

        await SafeQueueDeclareAsync(channel,
            queue: queue.Queue,
            durable: queue.Durable,
            exclusive: false,
            autoDelete: false,
            arguments: mainArgs);

        await channel.QueueBindAsync(
            queue.Queue,
            exchange,
            mainRoutingKey);

        // cria as retry queues (sem exchanges adicionais). Cada retry tem TTL e encaminha para o próximo routing key.
        for (int i = 0; i < retry.Delays.Length; i++)
        {
            var delay = retry.Delays[i];
            var retryIndex = i + 1;
            var retryQueueName = $"{queue.Queue}{retry.RetrySuffix}.{retryIndex}";
            var retryRoutingKey = $"{mainRoutingKey}.retry.{retryIndex}";
            var nextRoutingKey = (i == retry.Delays.Length - 1) ? deadRoutingKey : $"{mainRoutingKey}.retry.{retryIndex + 1}";

            var args = new Dictionary<string, object>
            {
                { "x-message-ttl", delay * 1000 },
                { "x-dead-letter-exchange", exchange },
                { "x-dead-letter-routing-key", nextRoutingKey }
            };

            await SafeQueueDeclareAsync(channel,
                queue: retryQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args);

            await channel.QueueBindAsync(
                retryQueueName,
                exchange,
                retryRoutingKey);
        }

        // fila final (dead) para mensagens definitivamente com falha
        var deadQueue = $"{queue.Queue}{retry.DeadSuffix}";
        await SafeQueueDeclareAsync(channel,
            queue: deadQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await channel.QueueBindAsync(
            deadQueue,
            exchange,
            deadRoutingKey);
    }

    private static async Task SafeQueueDeclareAsync(
        IChannel channel,
        string queue,
        bool durable,
        bool exclusive,
        bool autoDelete,
        IDictionary<string, object>? arguments)
    {
        try
        {
            await channel.QueueDeclareAsync(
                queue: queue,
                durable: durable,
                exclusive: exclusive,
                autoDelete: autoDelete,
                arguments: arguments);
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
        {
            // PRECONDITION_FAILED - inequivalent arg -> try delete and recreate
            var reply = ex.ShutdownReason?.ReplyText ?? string.Empty;
            if (reply.Contains("PRECONDITION_FAILED", StringComparison.OrdinalIgnoreCase) &&
                reply.Contains("inequivalent arg", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await channel.QueueDeleteAsync(queue, ifUnused: false, ifEmpty: false);
                }
                catch { }

                await channel.QueueDeclareAsync(
                    queue: queue,
                    durable: durable,
                    exclusive: exclusive,
                    autoDelete: autoDelete,
                    arguments: arguments);

                return;
            }

            throw;
        }
    }
}