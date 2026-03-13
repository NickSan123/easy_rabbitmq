using easy_rabbitmq.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace easy_rabbitmq.Consumer;

public class RabbitMQConsumer(IRabbitMQChannelPool channelPool) : IRabbitMQConsumer
{
    private readonly IRabbitMQChannelPool _channelPool = channelPool;

    public async Task ConsumeAsync<T>(
        string queue,
        Func<T, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var channel = await _channelPool.RentAsync(cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var message = JsonSerializer.Deserialize<T>(json);

                if (message != null)
                    await handler(message);

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch
            {
                await channel.BasicNackAsync(
                    ea.DeliveryTag,
                    false,
                    true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer);

        // mantém o consumer ativo
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }
}