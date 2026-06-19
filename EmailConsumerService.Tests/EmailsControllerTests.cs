using EmailConsumerService.Contracts.V1;
using EmailConsumerService.Controllers.V1;
using EmailConsumerService.Models;
using EmailConsumerService.Services.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EmailConsumerService.Tests;

public class EmailsControllerTests
{
    private readonly Mock<IKafkaEmailProducer> _emailProducer = new();

    private EmailsController CreateController() => new(_emailProducer.Object);

    private static SendEmailRequest CreateValidRequest() => new()
    {
        To = ["recipient@example.com"],
        Subject = "Test subject",
        Body = "Test body",
        Attachments =
        [
            new EmailAttachmentRequest
            {
                FileName = "report.pdf",
                ContentType = "application/pdf",
                ContentBase64 = "dGVzdA=="
            }
        ]
    };

    [Fact]
    public async Task SendEmail_MapsRequestAndProducesMessage()
    {
        var controller = CreateController();
        var request = CreateValidRequest();
        var cancellationToken = new CancellationTokenSource().Token;

        EmailMessage? produced = null;
        _emailProducer
            .Setup(producer => producer.ProduceAsync(It.IsAny<EmailMessage>(), cancellationToken))
            .Callback<EmailMessage, CancellationToken>((message, _) => produced = message)
            .ReturnsAsync("message-123");

        await controller.SendEmail(request, cancellationToken);

        Assert.NotNull(produced);
        Assert.Equal(request.To, produced.To);
        Assert.Equal("Test subject", produced.Subject);
        Assert.Equal("Test body", produced.Body);
        var attachment = Assert.Single(produced.Attachments);
        Assert.Equal("report.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal("dGVzdA==", attachment.ContentBase64);
    }

    [Fact]
    public async Task SendEmail_ReturnsAcceptedWithMessageId()
    {
        var controller = CreateController();
        var request = CreateValidRequest();

        _emailProducer
            .Setup(producer => producer.ProduceAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("message-123");

        var result = await controller.SendEmail(request, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
        var response = Assert.IsType<SendEmailResponse>(accepted.Value);
        Assert.Equal("message-123", response.MessageId);
    }

    [Fact]
    public async Task SendEmail_MapsAllRecipientAndContentFields()
    {
        var controller = CreateController();
        var request = new SendEmailRequest
        {
            To = ["a@example.com", "b@example.com"],
            From = "sender@example.com",
            FromName = "Sender Name",
            Cc = ["cc@example.com"],
            Bcc = ["bcc@example.com"],
            Subject = "Subject",
            Body = "<p>Body</p>",
            IsHtml = true
        };

        EmailMessage? produced = null;
        _emailProducer
            .Setup(producer => producer.ProduceAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => produced = message)
            .ReturnsAsync("message-123");

        await controller.SendEmail(request, CancellationToken.None);

        Assert.NotNull(produced);
        Assert.Equal(request.To, produced.To);
        Assert.Equal("sender@example.com", produced.From);
        Assert.Equal("Sender Name", produced.FromName);
        Assert.Equal(["cc@example.com"], produced.Cc);
        Assert.Equal(["bcc@example.com"], produced.Bcc);
        Assert.True(produced.IsHtml);
    }

    [Fact]
    public async Task SendEmail_WithoutAttachments_ProducesEmptyAttachmentList()
    {
        var controller = CreateController();
        var request = new SendEmailRequest
        {
            To = ["recipient@example.com"],
            Subject = "Subject",
            Body = "Body"
        };

        EmailMessage? produced = null;
        _emailProducer
            .Setup(producer => producer.ProduceAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => produced = message)
            .ReturnsAsync("message-123");

        await controller.SendEmail(request, CancellationToken.None);

        Assert.NotNull(produced);
        Assert.Empty(produced.Attachments);
    }
}
