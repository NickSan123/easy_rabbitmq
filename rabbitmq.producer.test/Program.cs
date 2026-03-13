using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Extensions;
using easy_rabbitmq.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddEasyRabbitMQ(options =>
        {
            options.HostName = "localhost";
            options.Port = 5672;
            options.UserName = "guest";
            options.Password = "guest";
            options.VirtualHost = "/";
            options.ClientProvidedName = "producer-test";
        });
    })
    .Build();

var services = host.Services;
var publisher = services.GetRequiredService<IRabbitMQPublisher>();
var pool = services.GetRequiredService<IRabbitMQChannelPool>();

// Define uma topologia de exemplo com retry
var topology = new RabbitMQTopology
{
    Exchange = "friendly.events",
    ExchangeType = easy_rabbitmq.Enums.RabbitMQExchangeType.Direct,
    Durable = true,
    Queues =
    [
        new() { Queue = "friendly.queue.offline", RoutingKey = "device.offline", Durable = true },
        new() { Queue = "friendly.queue.logs", RoutingKey = "device.logs", Durable = true }
    ],
    Retry = new RabbitMQRetryOptions
    {
        Enabled = true,
        Delays = [5, 10],
        RetrySuffix = ".retry",
        DeadSuffix = ".dead"
    }
};

// Declara exchanges/filas/topologia
var channel = await pool.RentAsync();
try
{
    await RabbitMQTopologyBuilder.DeclareAsync(channel, topology);
}
finally
{
    pool.Return(channel);
}

// sinaliza que a topologia foi inicializada (útil para o publisher local)
var topologyManager = services.GetRequiredService<easy_rabbitmq.Topology.TopologyManager>();
topologyManager.SetReady();

Console.WriteLine("Publicando mensagens de exemplo na exchange 'friendly.events'...");

// publica mensagens normais e algumas que devem falhar para acionar retry
for (int i = 1; i <= 10; i++)
{
    var msg = new DeviceMessage { Sn = i % 3 == 0 ? $"device-{i:000}-fail" : $"device-{i:000}" };
    await publisher.PublishAsync(exchange: topology.Exchange, routingKey: "device.offline", message: msg);
    Console.WriteLine($"Mensagem publicada: {msg.Sn}");
    await Task.Delay(300);
}

Console.WriteLine("Publicação finalizada. Pressione Enter para sair...");
Console.ReadLine();

public class DeviceMessage
{
    public string Sn { get; set; } = string.Empty;
}
