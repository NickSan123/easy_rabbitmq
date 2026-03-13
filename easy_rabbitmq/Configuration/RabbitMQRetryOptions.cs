namespace easy_rabbitmq.Configuration;

public class RabbitMQRetryOptions
{
    /// <summary>
    /// Ativa ou desativa o retry automático
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Delays em segundos para cada retry
    /// Ex: [10,30,60]
    /// </summary>
    public int[] Delays { get; set; } = [];

    /// <summary>
    /// Sufixo da fila de retry
    /// </summary>
    public string RetrySuffix { get; set; } = ".retry";

    /// <summary>
    /// Sufixo da fila final (dead)
    /// </summary>
    public string DeadSuffix { get; set; } = ".dead";
}