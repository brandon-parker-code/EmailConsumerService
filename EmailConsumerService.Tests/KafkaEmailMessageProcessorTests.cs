using Confluent.Kafka;
using EmailConsumerService.Models;
using EmailConsumerService.Services.Email;
using EmailConsumerService.Services.Kafka;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailConsumerService.Tests;

public class KafkaEmailMessageProcessorTests
{
    private readonly Mock<IEmailMessageHandler> _emailMessageHandler = new();
    private readonly Mock<IConsumer<string, string>> _consumer = new();
    private readonly Mock<ILogger<KafkaEmailMessageProcessor>> _logger = new();

    private KafkaEmailMessageProcessor CreateProcessor() =>
        new(_emailMessageHandler.Object, _consumer.Object, _logger.Object);

    private static ConsumeResult<string, string> CreateConsumeResult(string value) => new()
    {
        TopicPartitionOffset = new TopicPartitionOffset("email-requests", new Partition(0), new Offset(42)),
        Message = new Message<string, string> { Value = value }
    };

    [Fact]
    public async Task ProcessMessageAsync_WhenJsonInvalid_DoesNotHandleOrCommit()
    {
        var processor = CreateProcessor();
        var consumeResult = CreateConsumeResult("{ invalid json");

        await processor.ProcessMessageAsync(consumeResult, CancellationToken.None);

        _emailMessageHandler.Verify(
            handler => handler.HandleAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _consumer.Verify(consumer => consumer.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenJsonIsNull_DoesNotHandleOrCommit()
    {
        var processor = CreateProcessor();
        var consumeResult = CreateConsumeResult("null");

        await processor.ProcessMessageAsync(consumeResult, CancellationToken.None);

        _emailMessageHandler.Verify(
            handler => handler.HandleAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _consumer.Verify(consumer => consumer.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenHandlerSucceeds_HandlesAndCommits()
    {
        var processor = CreateProcessor();
        var consumeResult = CreateConsumeResult(
            """
            {
              "to": ["recipient@example.com"],
              "subject": "Test subject",
              "body": "Test body"
            }
            """);
        var cancellationToken = new CancellationTokenSource().Token;

        await processor.ProcessMessageAsync(consumeResult, cancellationToken);

        _emailMessageHandler.Verify(
            handler => handler.HandleAsync(
                It.Is<EmailMessage>(message =>
                    message.To.Single() == "recipient@example.com" &&
                    message.Subject == "Test subject" &&
                    message.Body == "Test body"),
                cancellationToken),
            Times.Once);
        _consumer.Verify(consumer => consumer.Commit(consumeResult), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenHandlerFails_DoesNotCommit()
    {
        var processor = CreateProcessor();
        var consumeResult = CreateConsumeResult(
            """
            {
              "to": ["recipient@example.com"],
              "subject": "Test subject",
              "body": "Test body"
            }
            """);

        _emailMessageHandler
            .Setup(handler => handler.HandleAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Send failed"));

        await processor.ProcessMessageAsync(consumeResult, CancellationToken.None);

        _emailMessageHandler.Verify(
            handler => handler.HandleAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _consumer.Verify(consumer => consumer.Commit(It.IsAny<ConsumeResult<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenMessageHasAttachments_PassesThemToHandler()
    {
        var processor = CreateProcessor();
        var consumeResult = CreateConsumeResult(
            """
            {
              "to": ["recipient@example.com"],
              "subject": "With attachment",
              "body": "See attached",
              "attachments": [
                {
                  "fileName": "report.pdf",
                  "contentType": "application/pdf",
                  "contentBase64": "dGVzdA=="
                }
              ]
            }
            """);
        var cancellationToken = new CancellationTokenSource().Token;

        await processor.ProcessMessageAsync(consumeResult, cancellationToken);

        _emailMessageHandler.Verify(
            handler => handler.HandleAsync(
                It.Is<EmailMessage>(message =>
                    message.Attachments.Count == 1 &&
                    message.Attachments[0].FileName == "report.pdf" &&
                    message.Attachments[0].ContentType == "application/pdf" &&
                    message.Attachments[0].ContentBase64 == "dGVzdA=="),
                cancellationToken),
            Times.Once);
        _consumer.Verify(consumer => consumer.Commit(consumeResult), Times.Once);
    }
}
