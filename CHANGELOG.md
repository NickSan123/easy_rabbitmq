# Changelog

Todas as alterações notáveis neste projeto serão documentadas neste arquivo.
Seguindo o formato "Keep a Changelog" e versionamento semântico (SemVer).

## [1.0.0] - 2026-03-14
### Added
- Implementação inicial da biblioteca `easy_rabbitmq` para .NET 10
  - Abstrações: `IRabbitMQConnection`, `IRabbitMQChannelFactory`, `IRabbitMQChannelPool`, `IRabbitMQPublisher`.
  - Conexão resiliente (RabbitMQConnection) com Options pattern (RabbitMQOptions).
  - Pool de canais assíncrono (RabbitMQChannelPool) com limite de canais e reuso.
  - Fábrica de canais (RabbitMQChannelFactory).
  - Publisher (RabbitMQPublisher) com suporte a publisher confirms usando canais criados via `CreateChannelOptions` do `RabbitMQ.Client`.
  - Topology builder (`RabbitMQTopologyBuilder`) para declarar exchange/filas/queues de retry com TTL + DLX e fila dead.
  - TopologyManager para sincronizar readiness entre declaração de topologia e publishers.
  - Consumer starter (RabbitMQConsumerStarter) e exemplo de consumer manual.
  - Projetos de exemplo: `rabbitmq.producer.test` e `rabbitmq.consumer.test`.
  - README e LICENSE (MIT) adicionados.

### Changed
- README alinhado com a implementação atual, incluindo exemplos e instruções de uso e publicação.

### Known issues / Notes
- RabbitMQTopologyBuilder tenta lidar com PRECONDITION_FAILED ao redeclarar filas (delete + recreate) — esse comportamento é útil em ambientes de desenvolvimento; em produção, recomenda-se migrar filas/versões em vez de deletar automaticamente.

### Environment
- Target: .NET 10
- Pacotes: RabbitMQ.Client 7.2.1
