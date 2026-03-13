using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddEasyRabbitMQ(options =>
        {
            options.HostName = "localhost";
            options.Port = 5672;
            options.UserName = "guest";
            options.Password = "guest";
            options.VirtualHost = "/";
            options.ClientProvidedName = "producer-test";
        });
    })
    .Build();

var publisher = host.Services.GetRequiredService<IRabbitMQPublisher>();

Console.WriteLine("Publicando mensagens...");



    //await publisher.PublishAsync(
    //    exchange: "botwatchman.events",
    //    routingKey: "botwatchman.#",
    //    message: item);

    

Console.WriteLine("Fim");
Console.ReadLine();

public class events
{
    public int Id { get; set; }
    public DateTime timestamp { get; set; }
    public object data { get; set; }
}