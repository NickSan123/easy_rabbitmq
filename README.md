Easy RabbitMQ 🐇
Uma biblioteca leve, resiliente e de alta performance para integração com RabbitMQ em .NET. O Easy RabbitMQ abstrai a complexidade de gerenciamento de conexões, pooling de canais e criação de topologia, permitindo que você foque no que importa: a lógica de negócio.
---
📦 Instalação
A biblioteca é dividida em dois pacotes:
🔹 Core (obrigatório)
```bash
dotnet add package easy_rabbitmq
```
🔹 Hosting (opcional, recomendado para aplicações)
```bash
dotnet add package easy_rabbitmq.Hosting
```
> 💡 O pacote `easy_rabbitmq.Hosting` adiciona integração com `IHostedService` para iniciar automaticamente os consumidores.
---
🚀 Guia de Configuração
1. Definindo a Topologia
Diferente de outras bibliotecas, o Easy RabbitMQ permite que você defina a infraestrutura (Exchanges e Queues) de forma centralizada.
```csharp
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Enums;

var topology = new RabbitMQTopology 
{ 
    Exchange = "vendas.events", 
    ExchangeType = RabbitMQExchangeType.Direct, 
    Durable = true, 
    Queues = [ 
        new() { Queue = "processar-pedido", RoutingKey = "pedido.criado", Durable = true }
    ], 
    Retry = new RabbitMQRetryOptions 
    { 
        Enabled = true, 
        Delays = [5, 10, 30], // segundos
        RetrySuffix = ".retry",
        DeadSuffix = ".dead"
    } 
};
```
---
2. Registro no Container de DI
No seu `Program.cs`:
```csharp
using easy_rabbitmq.Extensions;
using easy_rabbitmq.Hosting.Extensions;

builder.Services.AddEasyRabbitMQ(options => 
{ 
    options.HostName = "localhost"; 
    options.UserName = "guest"; 
    options.Password = "guest"; 
    options.ClientProvidedName = "minha-aplicacao"; 
}, topology);

// 🔥 Opcional: inicia automaticamente os consumers
builder.Services.AddEasyRabbitMQHosting();

// 🔍 Scan automático dos handlers
builder.Services.AddRabbitMQConsumersFromAssembly(typeof(Program).Assembly);
```
---
📥 Consumindo Mensagens (Consumer)
Crie uma classe que implemente `IRabbitMQHandler<T>` e utilize o atributo `[RabbitMQConsumer]`.
```csharp
using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Consumer;

[RabbitMQConsumer(
    exchange: "vendas.events", 
    queue: "processar-pedido", 
    routingKey: "pedido.criado")]
public class PedidoHandler(
    ILogger<PedidoHandler> logger, 
    IBancoDeDados db) : IRabbitMQHandler<PedidoDto>
{
    public async Task HandleAsync(PedidoDto message)
    {
        logger.LogInformation("Processando pedido: {Id}", message.Id);

        // Se lançar exceção → entra no retry automaticamente
        await db.SalvarPedidoAsync(message);
    }
}
```
---
📤 Publicando Mensagens (Producer)
Injete `IRabbitMQPublisher`:
```csharp
public class CheckoutService(IRabbitMQPublisher publisher)
{
    public async Task FinalizarCompra(Pedido pedido)
    {
        var evento = new PedidoDto 
        { 
            Id = pedido.Id, 
            Valor = pedido.Total 
        };

        await publisher.PublishAsync(
            exchange: "vendas.events",
            routingKey: "pedido.criado",
            message: evento
        );
    }
}
```
---
⚙️ Modo sem Hosting (avançado)
Se você não quiser usar `easy_rabbitmq.Hosting`, pode iniciar manualmente:
```csharp
public class Worker : BackgroundService
{
    private readonly RabbitMQConsumerStarter _starter;

    public Worker(RabbitMQConsumerStarter starter)
    {
        _starter = starter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _starter.StartAsync(stoppingToken);
    }
}
```
---
🛠️ Arquitetura
📦 Separação por camadas
Pacote	Responsabilidade
`easy_rabbitmq`	Core (conexão, publisher, consumer, pooling)
`easy_rabbitmq.Hosting`	Integração com .NET Hosting (`IHostedService`)
---
🔥 Diferenciais
Resiliência com Retry + Dead Letter
Retry automático com delays configuráveis
Fila `.dead` para mensagens não processadas
Alta Performance
Pool de canais (`Channel Pool`)
Redução de overhead de conexão
Simplicidade
Scan automático de consumers
Baixo acoplamento
Configuração centralizada
---
🎯 Conclusão
O Easy RabbitMQ segue boas práticas modernas de arquitetura:
Separação entre Core e Hosting
Baixo acoplamento
Alta performance
Fácil uso
---
Desenvolvido para simplificar sistemas distribuídos em .NET 🚀