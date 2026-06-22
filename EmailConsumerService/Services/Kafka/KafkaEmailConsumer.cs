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
        // Yield before the blocking consume loop so the host can finish startup.
        // consumer.Consume(...) blocks synchronously; without this yield the call
        // runs inline during BackgroundService startup and stalls Kestrel until
        // the first Kafka event arrives.
        await Task.Yield();

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
                    if (consumeResult is null || consumeResult.IsPartitionEOF)
                    {
                        continue;
                    }

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
