
## easy_rabbitmq

Biblioteca leve para facilitar o uso do RabbitMQ em aplicações .NET 10 (C# 14).

### Recursos principais

- Abstrações para conexão (`IRabbitMQConnection`) e pool de canais (`IRabbitMQChannelPool`).
- Publisher (`IRabbitMQPublisher`) com suporte a confirmações de publicação (publisher confirms) usando a API moderna do `RabbitMQ.Client`.
- Helpers para declarar topologia (`RabbitMQTopologyBuilder`), incluindo filas de retry (TTL + DLX).
- Starter de consumers que registra consumidores anotados e um exemplo de consumer manual.
- `TopologyManager` para coordenar readiness entre declaração de topologia e publishers.

### Instalação via NuGet

```bash
dotnet add package easy_rabbitmq
```

### Repositório

Código-fonte disponível em [`github.com/NickSan123/easy_rabbitmq`](https://github.com/NickSan123/easy_rabbitmq).

### Registro de serviços

No `HostBuilder`, registre a biblioteca via `AddEasyRabbitMQ` (Options pattern):

```csharp
services.AddEasyRabbitMQ(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.VirtualHost = "/";
    options.ClientProvidedName = "my-app";
});
```

O método `AddEasyRabbitMQ` registra as implementações padrão:
- `IRabbitMQConnection` -> `RabbitMQConnection`
- `IRabbitMQChannelFactory` -> `RabbitMQChannelFactory`
- `IRabbitMQChannelPool` -> `RabbitMQChannelPool`
- `IRabbitMQPublisher` -> `RabbitMQPublisher`
- `RabbitMQConsumerStarter`, `IRabbitMQConsumer` e um `IHostedService` para iniciar consumers
- `TopologyManager` (singleton)

### Exemplo simples de uso (producer + consumer)

```csharp
using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Extensions;
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
            options.ClientProvidedName = "my-simple-app";
        });
    })
    .Build();

var services = host.Services;

// producer
var publisher = services.GetRequiredService<IRabbitMQPublisher>();
await publisher.PublishAsync(
    exchange: "example.events",
    routingKey: "device.offline",
    message: new { Sn = "device-001" });

// consumer simples (manual) para a mesma fila
var pool = services.GetRequiredService<IRabbitMQChannelPool>();
var channel = await pool.RentAsync();

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, ea) =>
{
    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
    Console.WriteLine($"[simple-consumer] Recebido: {json}");
    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
};

await channel.BasicConsumeAsync(
    queue: "example.queue.offline",
    autoAck: false,
    consumer: consumer);

Console.ReadLine();
```

### Exemplo completo (producer + consumer com topologia e retry)

```csharp
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
            options.ClientProvidedName = "my-complex-app";
            options.AutomaticRecoveryEnabled = true;
            options.RequestedHeartbeat = 30;
            options.RequestedConnectionTimeout = 15000;
        });

        services.AddRabbitMQConsumersFromAssembly(typeof(Program).Assembly);
    })
    .Build();

var services = host.Services;

var topology = new RabbitMQTopology
{
    Exchange = "example.events",
    ExchangeType = easy_rabbitmq.Enums.RabbitMQExchangeType.Direct,
    Durable = true,
    Queues =
    [
        new() { Queue = "example.queue.offline", RoutingKey = "device.offline", Durable = true },
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

var pool = services.GetRequiredService<IRabbitMQChannelPool>();
var channel = await pool.RentAsync();
try
{
    await RabbitMQTopologyBuilder.DeclareAsync(channel, topology);
}
finally
{
    pool.Return(channel);
}

var topologyManager = services.GetRequiredService<easy_rabbitmq.Topology.TopologyManager>();
topologyManager.SetReady();

var publisher = services.GetRequiredService<IRabbitMQPublisher>();

for (int i = 1; i <= 3; i++)
{
    var msg = new { Sn = i % 2 == 0 ? $"device-{i:000}-fail" : $"device-{i:000}" };
    await publisher.PublishAsync(exchange: topology.Exchange, routingKey: "device.offline", message: msg);
}

await host.RunAsync();

// exemplo de consumer automático usando atributo

using easy_rabbitmq.Consumer;

public class DeviceMessage
{
    public string Sn { get; set; } = string.Empty;
}

[RabbitMQConsumer(queue: "example.queue.offline", exchange: "example.events", routingKey: "device.offline")]
public class DeviceOfflineConsumer : IRabbitMQMessageConsumer<DeviceMessage>
{
    public Task HandleAsync(DeviceMessage message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[auto-consumer] Recebido: {message.Sn}");
        return Task.CompletedTask;
    }
}
```

### Topologia e retries

Use `RabbitMQTopology` e `RabbitMQTopologyBuilder` para declarar a topologia do sistema. A abordagem padrão é:

- Um exchange principal.
- Cada fila principal com `x-dead-letter-exchange` apontando para o mesmo exchange e `x-dead-letter-routing-key` para a primeira fila de retry.
- Filas de retry com `x-message-ttl` e `x-dead-letter-routing-key` apontando para a próxima fila (ou para `.dead`).
- Uma fila final `.dead` recebendo mensagens que esgotaram os retries.

Exemplo:

```csharp
var topology = new RabbitMQTopology
{
    Exchange = "example.events",
    ExchangeType = RabbitMQExchangeType.Direct,
    Durable = true,
    Queues = new List<RabbitMQQueueTopology>
    {
        new() { Queue = "example.queue.offline", RoutingKey = "device.offline", Durable = true }
    },
    Retry = new RabbitMQRetryOptions
    {
        Enabled = true,
        Delays = new[] { 5, 10 }, // segundos
        RetrySuffix = ".retry",
        DeadSuffix = ".dead"
    }
};

var ch = await pool.RentAsync();
try { await RabbitMQTopologyBuilder.DeclareAsync(ch, topology); }
finally { pool.Return(ch); }

// se você declarar topologia manualmente (producer standalone), sinalize readiness:
var topologyManager = services.GetRequiredService<TopologyManager>();
topologyManager.SetReady();
```

