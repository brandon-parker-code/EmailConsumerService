using Confluent.Kafka;
using EmailConsumerService.Configuration;
using EmailConsumerService.Services.Kafka;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailConsumerService.Tests;

public class KafkaEmailConsumerTests
{
    private readonly Mock<IConsumer<string, string>> _consumer = new();
    private readonly Mock<IKafkaEmailMessageProcessor> _messageProcessor = new();
    private readonly Mock<ILogger<KafkaEmailConsumer>> _logger = new();

    private static readonly KafkaOptions Options = new()
    {
        Topic = "email-requests",
        GroupId = "email-consumer-service"
    };

    private KafkaEmailConsumer CreateConsumer() =>
        new(_consumer.Object, _messageProcessor.Object, Options, _logger.Object);

    private static ConsumeResult<string, string> EofResult() =>
        new() { IsPartitionEOF = true };

    private static ConsumeResult<string, string> MessageResult() =>
        new()
        {
            IsPartitionEOF = false,
            Message = new Message<string, string> { Key = "key", Value = "{}" }
        };

    [Fact]
    public async Task StartAsync_WhenResultIsPartitionEof_DoesNotProcessAndKeepsConsuming()
    {
        var cts = new CancellationTokenSource();

        // First poll yields an EOF marker (should be skipped); the second poll
        // cancels and throws to break the loop cleanly.
        _consumer.SetupSequence(consumer => consumer.Consume(It.IsAny<CancellationToken>()))
            .Returns(EofResult())
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        await CreateConsumer().StartAsync(cts.Token);

        _messageProcessor.Verify(
            processor => processor.ProcessMessageAsync(
                It.IsAny<ConsumeResult<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _consumer.Verify(consumer => consumer.Close(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenResultIsMessage_ProcessesIt()
    {
        var cts = new CancellationTokenSource();
        var message = MessageResult();

        _consumer.SetupSequence(consumer => consumer.Consume(It.IsAny<CancellationToken>()))
            .Returns(message)
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        await CreateConsumer().StartAsync(cts.Token);

        _messageProcessor.Verify(
            processor => processor.ProcessMessageAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenConsumeThrowsConsumeException_LogsAndContinues()
    {
        var cts = new CancellationTokenSource();
        var message = MessageResult();
        var consumeException = new ConsumeException(
            new ConsumeResult<byte[], byte[]>(), new Error(ErrorCode.Local_Fail));

        _consumer.SetupSequence(consumer => consumer.Consume(It.IsAny<CancellationToken>()))
            .Throws(consumeException)
            .Returns(message)
            .Returns(() =>
            {
                cts.Cancel();
                throw new OperationCanceledException();
            });

        await CreateConsumer().StartAsync(cts.Token);

        // Processing still happens for the message that follows the transient error.
        _messageProcessor.Verify(
            processor => processor.ProcessMessageAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
