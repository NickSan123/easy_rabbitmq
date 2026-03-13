using easy_rabbitmq.Abstractions;
using System.Text;
using System.Text.Json;

namespace easy_rabbitmq.Serialization;

internal class JsonMessageSerializer : IMessageSerializer
{
    public byte[] Serialize<T>(T message)
    => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

    public T Deserialize<T>(byte[] body)
        => JsonSerializer.Deserialize<T>(body)!;
}
