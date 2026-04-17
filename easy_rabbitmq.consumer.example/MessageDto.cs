using System.Text.Json.Serialization;

namespace easy_rabbitmq.consumer.example;

public class MessageDto
{
    [JsonPropertyName("serial")]
    public string? Serial { get; set; }
}
