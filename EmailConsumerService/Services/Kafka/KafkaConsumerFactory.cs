using Confluent.Kafka;
using EmailConsumerService.Configuration;

namespace EmailConsumerService.Services.Kafka;

public static class KafkaConsumerFactory
{
    public static IConsumer<string, string> Create(KafkaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka:BootstrapServers must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Topic))
        {
            throw new InvalidOperationException("Kafka:Topic must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.GroupId))
        {
            throw new InvalidOperationException("Kafka:GroupId must be configured.");
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = ParseAutoOffsetReset(options.AutoOffsetReset),
            EnableAutoCommit = false
        };

        return new ConsumerBuilder<string, string>(consumerConfig).Build();
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        value.Equals("Latest", StringComparison.OrdinalIgnoreCase)
            ? AutoOffsetReset.Latest
            : AutoOffsetReset.Earliest;
}
