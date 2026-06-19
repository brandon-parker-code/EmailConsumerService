using System.Text.Json;
using Confluent.Kafka;
using EmailConsumerService.Configuration;
using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Kafka;

public class KafkaEmailProducer(
    IProducer<string, string> producer,
    KafkaOptions options,
    ILogger<KafkaEmailProducer> logger) : IKafkaEmailProducer, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> ProduceAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid().ToString();
        var value = JsonSerializer.Serialize(message, JsonOptions);

        var result = await producer.ProduceAsync(
            options.Topic,
            new Message<string, string> { Key = messageId, Value = value },
            cancellationToken);

        logger.LogInformation(
            "Produced email message {MessageId} to {TopicPartitionOffset}.",
            messageId,
            result.TopicPartitionOffset);

        return messageId;
    }

    public void Dispose()
    {
        producer.Flush(TimeSpan.FromSeconds(5));
        producer.Dispose();
    }
}
