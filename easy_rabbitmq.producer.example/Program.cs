using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.consumer.example;
using easy_rabbitmq.Extensions;
using easy_rabbitmq.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {

        var topology = new RabbitMQTopology
        {
            Exchange = "example.events",
            ExchangeType = easy_rabbitmq.Enums.RabbitMQExchangeType.Direct,
            Durable = true,
            Queues =
    [
        new() { Queue = "example.queue.last", RoutingKey = "device.last", Durable = true },
        new() { Queue = "example.queue.logs", RoutingKey = "device.logs", Durable = true }
    ],
            Retry = new RabbitMQRetryOptions
            {
                Enabled = true,
                Delays = [5, 10],
                RetrySuffix = ".retry",
                DeadSuffix = ".dead"
            }
        };

        services.AddEasyRabbitMQ(options =>
        {
            options.HostName = "localhost";
            options.Port = 5672;
            options.UserName = "guest";
            options.Password = "guest";
            options.VirtualHost = "/";
            options.ClientProvidedName = "producer-test";
        }, topology);
    })
    .Build();

var services = host.Services;
var publisher = services.GetRequiredService<IRabbitMQPublisher>();
var pool = services.GetRequiredService<IRabbitMQChannelPool>();

//// Define uma topologia de exemplo com retry


//// Declara exchanges/filas/topologia
//var channel = await pool.RentAsync();
//try
//{
//    await RabbitMQTopologyBuilder.DeclareAsync(channel, topology);
//}
//finally
//{
//    pool.Return(channel);
//}

// sinaliza que a topologia foi inicializada (útil para o publisher local)
var topologyManager = services.GetRequiredService<TopologyManager>();
topologyManager.SetReady();

Console.WriteLine("Publicando mensagens de exemplo na exchange 'example.events'...");

// publica mensagens normais e algumas que devem falhar para acionar retry
for (int i = 1; i <= 10; i++)
{
    var msg = new MessageDto { Serial = i % 3 == 0 ? $"device-{i:000}-fail" : $"device-{i:000}" };
    await publisher.PublishAsync(exchange: "example.events", routingKey: "device.last", message: msg);
    Console.WriteLine($"Mensagem publicada: {msg.Serial}");
    await Task.Delay(300);
}

Console.WriteLine("Publicação finalizada. Pressione Enter para sair...");
Console.ReadLine();
