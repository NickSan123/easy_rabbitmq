## easy_rabbitmq

`easy_rabbitmq` é uma biblioteca de apoio para publicar, consumir e gerenciar topologia no RabbitMQ usando .NET, com foco em simplicidade, DI e boas práticas (reconexão, pooling de canais, etc.).

### Instalação

Adicione a referência ao projeto (ou ao pacote NuGet, se você o publicar) e depois registre os serviços no seu `Host`:

```csharp
services.AddEasyRabbitMQ(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.VirtualHost = "/";
    options.ClientProvidedName = "producer-test";
});
```

### Conexão e Pool de Canais

- **Interface**: `IRabbitMQConnection`  
- **Implementação**: `RabbitMQConnection`

Ela cria e mantém uma única instância de `IConnection` usando `ConnectionFactory.CreateConnectionAsync`, com:

- Heartbeat configurável
- Reconexão automática (`AutomaticRecoveryEnabled`)
- `ClientProvidedName` para facilitar identificação no management

Para canais:

- **Interface**: `IRabbitMQChannelPool`
- **Implementação**: `RabbitMQChannelPool`

O pool:

- Controla o número máximo de canais (`maxChannels`, padrão 20)
- Reaproveita canais abertos em um `ConcurrentBag<IChannel>`
- Cria canais novos via `IConnection.CreateChannelAsync(...)`
- **Habilita publisher confirms** usando `CreateChannelOptions`:

```csharp
var channelOptions = new CreateChannelOptions(
    publisherConfirmationsEnabled: true,
    publisherConfirmationTrackingEnabled: true,
    outstandingPublisherConfirmationsRateLimiter: null,
    consumerDispatchConcurrency: 1);

var channel = await conn.CreateChannelAsync(channelOptions, cancellationToken: ct);
```

Com isso, todos os canais usados pela biblioteca já vêm com confirmações de publisher configuradas de forma nativa pelo client RabbitMQ 7+.

### Publicação com Confirmação

- **Interface**: `IRabbitMQPublisher`
- **Implementação**: `RabbitMQPublisher`

O publisher:

1. Aguarda a topologia estar pronta via `TopologyManager.Ready` (útil quando um starter se encarrega de declarar exchanges/filas).
2. Aluga um canal do pool (`IRabbitMQChannelPool.RentAsync`).
3. Serializa a mensagem em JSON (`System.Text.Json`).
4. Publica usando `BasicPublishAsync` do próprio `IChannel`:

```csharp
await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken: cancellationToken);
```

Graças às opções de canal citadas acima, o próprio `BasicPublishAsync`:

- Aguarda o ACK/NACK do broker
- Lança uma exceção (por exemplo, `PublishException`) em caso de falha de confirmação

Assim você obtém publisher confirms de forma transparente, sem precisar lidar manualmente com `ConfirmSelect`, `BasicAcksAsync`/`BasicNacksAsync` ou dicionários de pendências.

### Exemplo de Producer (`rabbitmq.producer.test`)

O projeto `rabbitmq.producer.test` mostra um uso completo:

- Configuração do host e DI
- Criação de uma topologia (`RabbitMQTopology`) com:
  - Exchange (`friendly.events`)
  - Filas e routing keys
  - Opções de retry / dead-letter
- Declaração da topologia com `RabbitMQTopologyBuilder.DeclareAsync`
- Sinalização de prontidão via `TopologyManager.SetReady()`
- Publicação de mensagens:

```csharp
var publisher = services.GetRequiredService<IRabbitMQPublisher>();
await publisher.PublishAsync(exchange: topology.Exchange,
                             routingKey: "device.offline",
                             message: new DeviceMessage { Sn = "device-001" });
```

### Exemplo de Consumer

O projeto `rabbitmq.consumer.test` (e a classe `RabbitMQConsumer`) mostram como:

- Alugar um canal do pool
- Declarar/consumir filas
- Processar mensagens e lidar com ACK/NACK

### Boas Práticas e Erros Comuns

- **ConfirmSelect removido**  
  Em versões antigas do client RabbitMQ para .NET, era comum chamar `ConfirmSelect` diretamente no `IModel`. A partir do `RabbitMQ.Client 7+`, isso foi removido do `IChannel`; confirmações são habilitadas via `CreateChannelOptions`. A biblioteca já está adaptada para esse modelo.

- **Topologia não pronta**  
  Se você publicar imediatamente na inicialização, garantida a chamada para `TopologyManager.SetReady()` após a declaração da topologia (como no projeto `rabbitmq.producer.test`), para o publisher aguardar corretamente.

### Onde evoluir

- Expor configurações de:
  - Número máximo de canais (`maxChannels`)
  - `consumerDispatchConcurrency`
  - Estratégia de rate limit para confirmações
- Documentar detalhadamente as opções de retry / dead-letter na topologia.

