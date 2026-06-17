using Confluent.Kafka;

namespace EmailConsumerService.Services.Kafka;

public interface IKafkaEmailMessageProcessor
{
    Task ProcessMessageAsync(
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken);
}
