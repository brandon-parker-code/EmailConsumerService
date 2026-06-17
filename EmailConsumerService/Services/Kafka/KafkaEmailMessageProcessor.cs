using System.Text.Json;
using Confluent.Kafka;
using EmailConsumerService.Models;
using EmailConsumerService.Services.Email;

namespace EmailConsumerService.Services.Kafka;

public class KafkaEmailMessageProcessor(
    IEmailMessageHandler emailMessageHandler,
    IConsumer<string, string> consumer,
    ILogger<KafkaEmailMessageProcessor> logger) : IKafkaEmailMessageProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task ProcessMessageAsync(
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        EmailMessage? emailMessage;

        try
        {
            emailMessage = JsonSerializer.Deserialize<EmailMessage>(consumeResult.Message.Value, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "Failed to deserialize Kafka message at {TopicPartitionOffset}.",
                consumeResult.TopicPartitionOffset);
            return;
        }

        if (emailMessage is null)
        {
            logger.LogWarning(
                "Kafka message at {TopicPartitionOffset} deserialized to null.",
                consumeResult.TopicPartitionOffset);
            return;
        }

        try
        {
            await emailMessageHandler.HandleAsync(emailMessage, cancellationToken);
            consumer.Commit(consumeResult);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to process Kafka message at {TopicPartitionOffset}. Offset will not be committed.",
                consumeResult.TopicPartitionOffset);
        }
    }
}
