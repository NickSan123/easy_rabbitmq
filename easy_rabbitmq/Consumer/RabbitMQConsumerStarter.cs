using easy_rabbitmq.Abstractions;
using easy_rabbitmq.Configuration;
using easy_rabbitmq.Topology;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace easy_rabbitmq.Consumer
{
    public class RabbitMQConsumerStarter(
        IServiceProvider provider,
        IRabbitMQChannelFactory channelFactory)
    {
        private readonly IServiceProvider _provider = provider;
        private readonly IRabbitMQChannelFactory _channelFactory = channelFactory;

        public async Task StartAsync()
        {
            var consumers = RabbitMQConsumerScanner.GetConsumers();

            foreach (var consumerType in consumers)
            {
                var attr = consumerType
                    .GetCustomAttribute<RabbitMQConsumerAttribute>();

                var handler = _provider.GetRequiredService(consumerType);

                var channel = await _channelFactory.GetChannelAsync();

                await RabbitMQTopologyBuilder.DeclareAsync(channel,
                    new RabbitMQTopology
                    {
                        Exchange = attr.Exchange,
                        Queue = attr.Queue,
                        RoutingKey = attr.RoutingKey
                    });
            }
        }
    }
}
