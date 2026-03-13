using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Extensions;
using rabbitmq.consumer.test;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddEasyRabbitMQ(options =>
        {
            options.HostName = "localhost";
            options.ClientProvidedName = "device-consumer";
        });
    })
    .Build();

var consumer = host.Services.GetRequiredService<IRabbitMQConsumer>();

await consumer.ConsumeAsync<Devices>(
    "device.offline",
    async (message) =>
    {
        Console.WriteLine($"Device offline: {message.Sn}");

        await Task.CompletedTask;
    });

Console.ReadLine();