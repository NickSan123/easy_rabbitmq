using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Consumer;
using Microsoft.Extensions.Logging;

namespace easy_rabbitmq.consumer.example;


[RabbitMQConsumer(exchange: "example.events", queue: "example.queue", routingKey: "exemple.*")]
public class DeviceHandler(
    ILogger<DeviceHandler> logger) : IRabbitMQHandler<MessageDto>
{
    public async Task HandleAsync(MessageDto message)
    {
        logger.LogInformation("Processando atualização para dispositivo: {Serial}", message.Serial);

        try
        {
            // Simula um processamento assíncrono, como uma chamada a um serviço externo ou uma operação de banco de dados
            logger.LogInformation("Atualização processada com sucesso para dispositivo: {Serial}", message.Serial);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar atualização para o dispositivo {Serial}. Payload: {@Message}", message.Serial, message);
            throw;
        }
    }
}
