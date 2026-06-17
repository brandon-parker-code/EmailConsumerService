namespace EmailConsumerService.Configuration;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string Topic { get; set; } = "email-requests";

    public string GroupId { get; set; } = "email-consumer-service";

    public string AutoOffsetReset { get; set; } = "Earliest";
}
