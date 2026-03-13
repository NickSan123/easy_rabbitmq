namespace easy_rabbitmq.Abstractions;

public interface IMessageSerializer
{
    byte[] Serialize<T>(T message);
    T Deserialize<T>(byte[] body);
}
