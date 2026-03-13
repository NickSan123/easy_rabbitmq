using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Channel;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Extensions;
using easy_rabbitmq.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddEasyRabbitMQ(options =>
        {
            options.HostName = "localhost";
            options.ClientProvidedName = "device-consumer";
        });
    })
    .Build();

var services = host.Services;

// Define a mesma topologia do producer para garantir compatibilidade
var topology = new RabbitMQTopology
{
    Exchange = "friendly.events",
    ExchangeType = easy_rabbitmq.Enums.RabbitMQExchangeType.Direct,
    Durable = true,
    Queues = new List<RabbitMQQueueTopology>
    {
        new RabbitMQQueueTopology { Queue = "friendly.queue.offiline", RoutingKey = "device.offline", Durable = true }
    },
    Retry = new RabbitMQRetryOptions
    {
        Enabled = true,
        Delays = new[] { 5, 10 },
        RetrySuffix = ".retry",
        DeadSuffix = ".dead"
    }
};

var pool = services.GetRequiredService<IRabbitMQChannelPool>();
var channel = await pool.RentAsync();
try
{
    // Declara a topologia (exchanges, filas, bindings e retry)
    await RabbitMQTopologyBuilder.DeclareAsync(channel, topology);

    // Cria um consumer manual que rejeita (nack) mensagens com sufixo '-fail' para acionar retry
    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += async (model, ea) =>
    {
        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<DeviceMessage>(json);

            if (message == null)
            {
                await channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            Console.WriteLine($"Recebido: {message.Sn}");

            if (message.Sn.Contains("-fail"))
            {
                Console.WriteLine("Mensagem marcada como falha. Rejeitando para enviar ao fluxo de retry...");
                // requeue = false -> envia para dead-letter / retry exchange configurado
                await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            // processamento bem-sucedido
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar: {ex.Message}");
            // rejeita sem requeue para acionar dead-letter
            await channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }
    };

    await channel.BasicQosAsync(0, 10, false);
    await channel.BasicConsumeAsync(queue: topology.Queues[0].Queue, autoAck: false, consumer: consumer);

    Console.WriteLine("Consumer manual iniciado. Pressione Enter para sair...");
    Console.ReadLine();
}
finally
{
    pool.Return(channel);
}

public class DeviceMessage
{
    public string Sn { get; set; } = string.Empty;
}
