using System.Reflection;

namespace easy_rabbitmq.Consumer;

public static class RabbitMQConsumerScanner
{
    public static IEnumerable<Type> GetConsumers()
    {
        var assemblies = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic);

        var types = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                types.AddRange(ex.Types.Where(t => t != null)!);
            }
        }

        return types.Where(t =>
            t is { IsClass: true, IsAbstract: false } &&
            t.GetCustomAttribute<RabbitMQConsumerAttribute>() != null);
    }

    public static IEnumerable<Type> GetConsumers(Assembly assembly)
    {
        try
        {
            return assembly
                .GetTypes()
                .Where(t =>
                    t is { IsClass: true, IsAbstract: false } &&
                    t.GetCustomAttribute<RabbitMQConsumerAttribute>() != null);
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(t =>
                    t is { IsClass: true, IsAbstract: false } &&
                    t.GetCustomAttribute<RabbitMQConsumerAttribute>() != null)!;
        }
    }
}