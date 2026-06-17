using EmailConsumerService.Services.Kafka;

namespace EmailConsumerService;

public class Worker(IKafkaEmailConsumer kafkaEmailConsumer, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email consumer worker starting.");
        await kafkaEmailConsumer.StartAsync(stoppingToken);
    }
}
