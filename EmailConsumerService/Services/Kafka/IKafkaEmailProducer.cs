using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Kafka;

public interface IKafkaEmailProducer
{
    Task<string> ProduceAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
