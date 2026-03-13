namespace easy_rabbitmq.Consumer;

public static class RabbitMQConsumerScanner
{
    public static IEnumerable<Type> GetConsumers()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t =>
                t.GetCustomAttributes(
                    typeof(RabbitMQConsumerAttribute),
                    false).Length != 0);
    }
}
