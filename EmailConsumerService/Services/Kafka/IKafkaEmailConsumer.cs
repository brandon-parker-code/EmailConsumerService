namespace EmailConsumerService.Services.Kafka;

public interface IKafkaEmailConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
}
