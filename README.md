# Easy RabbitMQ 🐇

Uma biblioteca leve, resiliente e de alta performance para integração com RabbitMQ em .NET. O **Easy RabbitMQ** abstrai a complexidade de gerenciamento de conexões, pooling de canais e criação de topologia, permitindo que você foque no que importa: a lógica de negócio.

## 📦 Instalação

Adicione o pacote ao seu projeto via .NET CLI:

```bash
dotnet add package easy_rabbitmq
```

---

## 🚀 Guia de Configuração

### 1. Definindo a Topologia
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
        Delays = [5, 10, 30], // Segundos entre tentativas
        RetrySuffix = ".retry",
        DeadSuffix = ".dead"
    } 
};
```

### 2. Registro no Container de DI
No seu `Program.cs`, registre a biblioteca passando as opções de conexão e a topologia definida:

```csharp
using easy_rabbitmq.Extensions;

builder.Services.AddEasyRabbitMQ(options => 
{ 
    options.HostName = "localhost"; 
    options.UserName = "guest"; 
    options.Password = "guest"; 
    options.ClientProvidedName = "minha-aplicacao"; 
}, topology); 

// Registra automaticamente todos os Handlers do projeto
builder.Services.AddRabbitMQConsumersFromAssembly(typeof(Program).Assembly);
```

---

## 📥 Consumindo Mensagens (Consumer)

Para consumir mensagens, basta criar uma classe que implemente `IRabbitMQHandler<T>` e decorá-la com o atributo `[RabbitMQConsumer]`. 

A biblioteca gerencia o escopo de injeção de dependência para você, permitindo injetar serviços `Scoped` ou `Transient` diretamente no construtor.

```csharp
using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Consumer;

[RabbitMQConsumer(exchange: "vendas.events", queue: "processar-pedido", routingKey: "pedido.criado")] 
public class PedidoHandler(
    ILogger<PedidoHandler> logger, 
    IBancoDeDados db) : IRabbitMQHandler<PedidoDto> 
{ 
    public async Task HandleAsync(PedidoDto message) 
    { 
        logger.LogInformation("Processando pedido: {Id}", message.Id); 
        
        // Se este método lançar uma exceção, o sistema de Retry entrará em ação automaticamente
        await db.SalvarPedidoAsync(message); 
    } 
}
```

---

## 📤 Publicando Mensagens (Producer)

Injete `IRabbitMQPublisher` em seus serviços ou APIs para enviar mensagens de forma eficiente utilizando o **Channel Pool**.

```csharp
public class CheckoutService(IRabbitMQPublisher publisher)
{
    public async Task FinalizarCompra(Pedido pedido)
    {
        var evento = new PedidoDto { Id = pedido.Id, Valor = pedido.Total };

        // Publica na exchange definida na topologia
        await publisher.PublishAsync(
            exchange: "vendas.events",
            routingKey: "pedido.criado",
            message: evento
        );
    }
}
```

---

## 🛠️ Arquitetura e Diferenciais

### **Resiliência com Retry e Dead Letter**
Ao ativar o `Retry` na topologia, o Easy RabbitMQ cria automaticamente uma estrutura robusta:
1. **Fila Principal**: Onde o processamento ocorre em tempo real.
2. **Filas de Retry**: Caso o Handler falhe, a mensagem é movida para filas de atraso (TTL) antes de retornar para a principal.
3. **Fila .dead**: Se todas as tentativas falharem, a mensagem é movida para esta fila para auditoria manual, evitando perda de dados.

### **Alta Performance (Channel Pooling)**
Abrir e fechar canais AMQP é custoso. Nossa biblioteca implementa um `IRabbitMQChannelPool` que reutiliza canais abertos, reduzindo drasticamente o overhead de CPU e latência de rede.

### **Escaneamento Automático**
O método `AddRabbitMQConsumersFromAssembly` elimina a necessidade de registrar cada Handler manualmente. Basta criar a classe e colocar o atributo que ela começará a processar mensagens assim que o app iniciar.

---
Desenvolvido para simplificar sistemas distribuídos em .NET com as melhores práticas de mercado.