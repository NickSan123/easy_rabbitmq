using System;
using System.Collections.Generic;
using System.Text;

namespace easy_rabbitmq.Abstractions;

public interface IRabbitMQConsumer
{
    Task ConsumeAsync<T>(
       string queue,
       Func<T, Task> handler,
       CancellationToken cancellationToken = default);
}
