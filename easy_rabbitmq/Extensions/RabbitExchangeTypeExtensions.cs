using easy_rabbitmq.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace easy_rabbitmq.Extensions;

public static class RabbitMQExchangeTypeExtensions
{
    public static string ToExchangeString(this RabbitMQExchangeType type)
    {
        return type switch
        {
            RabbitMQExchangeType.Direct => "direct",
            RabbitMQExchangeType.Fanout => "fanout",
            RabbitMQExchangeType.Topic => "topic",
            RabbitMQExchangeType.Headers => "headers",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
