using System;
using System.Collections.Generic;
using System.Text;

namespace easy_rabbitmq.Enums;

public enum RabbitMQExchangeType
{
    Direct,
    Fanout,
    Topic,
    Headers
}
