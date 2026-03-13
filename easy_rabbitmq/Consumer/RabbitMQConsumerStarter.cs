using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace easy_rabbitmq.Consumer;

public class RabbitMQConsumerStarter(
    IServiceProvider provider,
    IRabbitMQChannelFactory channelFactory,
    IOptions<RabbitMQOptions> options,
    easy_rabbitmq.Topology.TopologyManager topologyManager)
{
    private readonly IServiceProvider _provider = provider;
    private readonly IRabbitMQChannelFactory _channelFactory = channelFactory;
    private readonly RabbitMQOptions _options = options.Value;
    private readonly easy_rabbitmq.Topology.TopologyManager _topologyManager = topologyManager;

    public async Task StartAsync()
    {
        try
        {
            var consumers = RabbitMQConsumerScanner.GetConsumers();

            foreach (var consumerType in consumers)
            {
                var attr = consumerType.GetCustomAttribute<RabbitMQConsumerAttribute>();

                if (attr == null)
                    continue;

                var channel = await _channelFactory.GetChannelAsync();

                // controla quantas mensagens podem ficar pendentes
                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 10,
                    global: false);

                var topology = new RabbitMQTopology
                {
                    Exchange = attr.Exchange,
                    Retry = _options.Retry,
                    Queues = new List<RabbitMQQueueTopology>
                    {
                        new RabbitMQQueueTopology
                        {
                            Queue = attr.Queue,
                            RoutingKey = attr.RoutingKey
                        }
                    }
                };

                await RabbitMQTopologyBuilder.DeclareAsync(
                    channel,
                    topology);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (_, ea) =>
                {
                    using var scope = _provider.CreateScope();

                    try
                    {
                        var handler = scope.ServiceProvider
                            .GetRequiredService(consumerType);

                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);

                        var messageType = GetMessageType(consumerType);

                        object? message;

                        if (messageType != null)
                        {
                            message = JsonSerializer.Deserialize(json, messageType)
                                ?? throw new InvalidOperationException(
                                    $"Falha ao desserializar mensagem para {messageType.Name}");
                        }
                        else
                        {
                            message = json;
                        }

                        var method = consumerType.GetMethod(
                            "HandleAsync",
                            BindingFlags.Instance | BindingFlags.Public);

                        if (method == null)
                            throw new InvalidOperationException(
                                $"HandleAsync não encontrado em {consumerType.Name}");

                        var task = (Task?)method.Invoke(handler, new object?[] { message });

                        if (task != null)
                            await task;

                        await channel.BasicAckAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false);
                    }
                    catch (Exception)
                    {
                        await channel.BasicNackAsync(
                            deliveryTag: ea.DeliveryTag,
                            multiple: false,
                            requeue: false);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: attr.Queue,
                    autoAck: false,
                    consumerTag: string.Empty,
                    noLocal: false,
                    exclusive: false,
                    arguments: null,
                    consumer: consumer);
            }

            // sinaliza que a topologia foi criada com sucesso
            _topologyManager.SetReady();
        }
        catch (Exception ex)
        {
            _topologyManager.SetFailed(ex);
            throw;
        }
    }

    private static Type? GetMessageType(Type consumerType)
    {
        var interfaceType = consumerType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() ==
                typeof(IRabbitMQMessageConsumer<>));

        return interfaceType?.GetGenericArguments()[0];
    }
}