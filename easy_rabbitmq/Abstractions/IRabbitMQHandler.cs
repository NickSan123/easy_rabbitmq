using System;
using System.Collections.Generic;
using System.Text;

namespace easy_rabbitmq.Abstractions;

public interface IRabbitMQHandler<T>
{
    Task HandleAsync(T message);
}