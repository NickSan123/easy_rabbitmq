using System;
using System.Collections.Generic;
using System.Text;

namespace easy_rabbitmq.Configuration;

public class RabbitMQOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ClientProvidedName { get; set; } = "";
    public int RequestedConnectionTimeout { get; set; } = 30000;
    public int RequestedHeartbeat { get; set; } = 60;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public RabbitMQRetryOptions Retry { get; set; } = new();
}
