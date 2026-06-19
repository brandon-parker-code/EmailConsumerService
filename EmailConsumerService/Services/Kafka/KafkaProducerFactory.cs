using Confluent.Kafka;
using EmailConsumerService.Configuration;

namespace EmailConsumerService.Services.Kafka;

public static class KafkaProducerFactory
{
    public static IProducer<string, string> Create(KafkaOptions options) =>
        new ProducerBuilder<string, string>(CreateConfig(options)).Build();

    public static ProducerConfig CreateConfig(KafkaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka:BootstrapServers must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Topic))
        {
            throw new InvalidOperationException("Kafka:Topic must be configured.");
        }

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = string.IsNullOrWhiteSpace(options.ClientId) ? options.GroupId : options.ClientId,
            EnableIdempotence = options.EnableIdempotence,
            Acks = ParseAcks(options.Acks)
        };

        if (options.MessageMaxBytes is int messageMaxBytes)
        {
            producerConfig.MessageMaxBytes = messageMaxBytes;

        }

        if (!string.IsNullOrWhiteSpace(options.CompressionType))
        {
            producerConfig.CompressionType = ParseCompressionType(options.CompressionType);
        }

        return producerConfig;
    }

    private static Acks ParseAcks(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "all" or "-1" => Acks.All,
            "leader" or "1" => Acks.Leader,
            "none" or "0" => Acks.None,
            _ => Acks.All
        };

    private static CompressionType ParseCompressionType(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "gzip" => CompressionType.Gzip,
            "snappy" => CompressionType.Snappy,
            "lz4" => CompressionType.Lz4,
            "zstd" => CompressionType.Zstd,
            _ => CompressionType.None
        };
}
