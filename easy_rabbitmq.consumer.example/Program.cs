using easy_rabbitmq.Configuration;
using easy_rabbitmq.consumer.example;
using easy_rabbitmq.Extensions;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var topology = new RabbitMQTopology
{
    Exchange = "example.events",
    ExchangeType = easy_rabbitmq.Enums.RabbitMQExchangeType.Direct,
    Durable = true,
    Queues =
[
new() { Queue = "example.queue.last", RoutingKey = "device.last", Durable = true },
    new() { Queue = "example.queue.logs", RoutingKey = "device.logs", Durable = true }
],
    Retry = new RabbitMQRetryOptions
    {
        Enabled = true,
        Delays = [5, 10],
        RetrySuffix = ".retry",
        DeadSuffix = ".dead"
    }
};

builder.Services.AddEasyRabbitMQ(options =>
{
    options.HostName = builder.Configuration["rabbit_host_name"] ?? "localhost";
    options.Port = int.Parse(builder.Configuration["rabbit_port"] ?? "5672");
    options.UserName = builder.Configuration["rabbit_user_name"] ?? "guest";
    options.Password = builder.Configuration["rabbit_password"] ?? "guest";
    options.VirtualHost = builder.Configuration["rabbit_virtual_host"] ?? "/";
    options.ClientProvidedName = builder.Configuration["rabbit_client_provided_name"] ?? "bot-zamigos-last-device-consumer";
}, topology);

builder.Services.AddRabbitMQConsumersFromAssembly(typeof(DeviceHandler).Assembly);

var host = builder.Build();
host.Run();