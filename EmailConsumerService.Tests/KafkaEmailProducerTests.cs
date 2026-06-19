using System.Text.Json;
using Confluent.Kafka;
using EmailConsumerService.Configuration;
using EmailConsumerService.Models;
using EmailConsumerService.Services.Kafka;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailConsumerService.Tests;

public class KafkaEmailProducerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Mock<IProducer<string, string>> _producer = new();
    private readonly Mock<ILogger<KafkaEmailProducer>> _logger = new();

    private readonly KafkaOptions _options = new()
    {
        BootstrapServers = "localhost:9092",
        Topic = "email-requests",
        GroupId = "email-consumer-service"
    };

    private KafkaEmailProducer CreateProducer() =>
        new(_producer.Object, _options, _logger.Object);

    private static EmailMessage CreateMessage() => new()
    {
        To = ["recipient@example.com"],
        Subject = "Test subject",
        Body = "Test body",
        Attachments =
        [
            new EmailAttachment
            {
                FileName = "report.pdf",
                ContentType = "application/pdf",
                ContentBase64 = "dGVzdA=="
            }
        ]
    };

    [Fact]
    public async Task ProduceAsync_PublishesToConfiguredTopic()
    {
        Message<string, string>? capturedMessage = null;
        string? capturedTopic = null;

        _producer
            .Setup(producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((topic, message, _) =>
            {
                capturedTopic = topic;
                capturedMessage = message;
            })
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                TopicPartitionOffset = new TopicPartitionOffset(
                    "email-requests", new Partition(0), new Offset(1))
            });

        var producer = CreateProducer();
        var messageId = await producer.ProduceAsync(CreateMessage());

        Assert.Equal("email-requests", capturedTopic);
        Assert.NotNull(capturedMessage);
        Assert.Equal(messageId, capturedMessage.Key);
    }

    [Fact]
    public async Task ProduceAsync_ReturnsNonEmptyMessageId()
    {
        _producer
            .Setup(producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                TopicPartitionOffset = new TopicPartitionOffset(
                    "email-requests", new Partition(0), new Offset(1))
            });

        var producer = CreateProducer();
        var messageId = await producer.ProduceAsync(CreateMessage());

        Assert.True(Guid.TryParse(messageId, out _));
    }

    [Fact]
    public async Task ProduceAsync_SerializesMessageIncludingAttachments()
    {
        Message<string, string>? capturedMessage = null;

        _producer
            .Setup(producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, string>, CancellationToken>((_, message, _) =>
                capturedMessage = message)
            .ReturnsAsync(new DeliveryResult<string, string>
            {
                TopicPartitionOffset = new TopicPartitionOffset(
                    "email-requests", new Partition(0), new Offset(1))
            });

        var producer = CreateProducer();
        await producer.ProduceAsync(CreateMessage());

        Assert.NotNull(capturedMessage);
        var deserialized = JsonSerializer.Deserialize<EmailMessage>(capturedMessage.Value, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("Test subject", deserialized.Subject);
        Assert.Equal("recipient@example.com", deserialized.To.Single());
        var attachment = Assert.Single(deserialized.Attachments);
        Assert.Equal("report.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal("dGVzdA==", attachment.ContentBase64);
    }
}
