using Confluent.Kafka;
using EmailConsumerService.Configuration;

namespace EmailConsumerService.Services.Kafka;

public class KafkaEmailConsumer(
    IConsumer<string, string> consumer,
    IKafkaEmailMessageProcessor messageProcessor,
    KafkaOptions options,
    ILogger<KafkaEmailConsumer> logger) : IKafkaEmailConsumer, IDisposable
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        consumer.Subscribe(options.Topic);
        logger.LogInformation(
            "Subscribed to Kafka topic {Topic} using consumer group {GroupId}.",
            options.Topic,
            options.GroupId);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    await messageProcessor.ProcessMessageAsync(consumeResult, cancellationToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error on topic {Topic}.", options.Topic);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            consumer.Close();
            logger.LogInformation("Kafka consumer closed.");
        }
    }

    public void Dispose()
    {
        consumer.Dispose();
    }
}
