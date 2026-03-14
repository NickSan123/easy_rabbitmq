
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
dotnet add package NickSan123.EasyRabbitMQ
```

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

### Publicação e consumo

- Publisher: resolva `IRabbitMQPublisher` via DI e invoque:

```csharp
await publisher.PublishAsync(exchange: "exchange.name", routingKey: "my.key", message: myObj);
```

- Consumer:
  - Consumer manual (`rabbitmq.consumer.test`): usa `AsyncEventingBasicConsumer` diretamente e demonstra nack/ack e retry.
  - Consumer automático (`RabbitMQConsumerStarter`): escaneia tipos anotados (via atributo) e registra consumers via `AddRabbitMQConsumersFromAssembly`.

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
    Exchange = "friendly.events",
    ExchangeType = RabbitMQExchangeType.Direct,
    Durable = true,
    Queues = new List<RabbitMQQueueTopology>
    {
        new() { Queue = "friendly.queue.offline", RoutingKey = "device.offline", Durable = true }
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

