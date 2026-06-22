namespace EmailConsumerService.Configuration;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "email-requests";

    public string GroupId { get; set; } = "email-consumer-service";

    public string AutoOffsetReset { get; set; } = "Earliest";

    public string? ClientId { get; set; }

    public bool EnableIdempotence { get; set; } = true;

    public string Acks { get; set; } = "All";

    public string? CompressionType { get; set; }

    public int? MessageMaxBytes { get; set; }

    /// <summary>
    /// Maximum time (ms) the producer will spend delivering a message before it
    /// fails. Bounds how long the HTTP produce path can block when Kafka is
    /// unreachable (librdkafka's default is 300000 = 5 minutes).
    /// </summary>
    public int MessageTimeoutMs { get; set; } = 10_000;
}
