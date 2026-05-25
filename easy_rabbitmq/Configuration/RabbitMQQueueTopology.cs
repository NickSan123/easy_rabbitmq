using System;
using System.Collections.Generic;
using System.Text;

namespace easy_rabbitmq.Configuration
{
    public class RabbitMQQueueTopology
    {
        public string Queue { get; set; } = default!;
        public string RoutingKey { get; set; } = "#";
        public bool Durable { get; set; } = true;
        public bool Exclusive { get; set; } = false;
        public bool AutoDelete { get; set; } = false;
        public IDictionary<string, object?>? Arguments { get; set; } = null;
    }
}
