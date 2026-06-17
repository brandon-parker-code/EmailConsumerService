using EmailConsumerService.Configuration;
using EmailConsumerService.Models;
using EmailConsumerService.Services.Email;
using Microsoft.Extensions.Logging;
using Moq;
using SendGrid;
using System.Text.Json;

namespace EmailConsumerService.Tests;

public class SendGridEmailSenderTests
{
    private readonly Mock<ILogger<SendGridEmailSender>> _logger = new();

    private static SendGridOptions CreateOptions() => new()
    {
        ApiKey = "test-api-key",
        DefaultFromEmail = "noreply@example.com",
        DefaultFromName = "Example Sender"
    };

    private static EmailMessage CreateValidMessage() => new()
    {
        To = ["recipient@example.com"],
        Subject = "Test subject",
        Body = "Test body"
    };

    private SendGridEmailSender CreateSender(CaptureHttpHandler? handler = null)
    {
        handler ??= new CaptureHttpHandler();
        var httpClient = new HttpClient(handler);
        var client = new SendGridClient(httpClient, "test-api-key");
        return new SendGridEmailSender(CreateOptions(), client, _logger.Object);
    }

    [Fact]
    public async Task SendAsync_WhenFromMissingAndNoDefault_ThrowsInvalidOperationException()
    {
        var httpClient = new HttpClient(new CaptureHttpHandler());
        var client = new SendGridClient(httpClient, "test-api-key");
        var sender = new SendGridEmailSender(
            new SendGridOptions { ApiKey = "test-api-key" },
            client,
            _logger.Object);
        var message = CreateValidMessage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(message));

        Assert.Contains("SendGrid:DefaultFromEmail must be configured", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenAttachmentMissingFileName_ThrowsInvalidOperationException()
    {
        var sender = CreateSender();
        var message = CreateValidMessage();
        message.Attachments.Add(new EmailAttachment
        {
            FileName = "",
            ContentBase64 = Convert.ToBase64String("content"u8.ToArray())
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(message));

        Assert.Equal("Each attachment must include a file name.", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenAttachmentMissingContent_ThrowsInvalidOperationException()
    {
        var sender = CreateSender();
        var message = CreateValidMessage();
        message.Attachments.Add(new EmailAttachment
        {
            FileName = "file.txt",
            ContentBase64 = ""
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(message));

        Assert.Equal("Attachment 'file.txt' must include base64 content.", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenMessageHasAttachment_IncludesAttachmentInRequest()
    {
        var handler = new CaptureHttpHandler();
        var sender = CreateSender(handler);
        var message = CreateValidMessage();
        var contentBase64 = Convert.ToBase64String("hello"u8.ToArray());
        message.Attachments.Add(new EmailAttachment
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ContentBase64 = contentBase64
        });

        await sender.SendAsync(message);

        Assert.NotNull(handler.LastRequestBody);
        using var document = JsonDocument.Parse(handler.LastRequestBody);
        var attachment = document.RootElement.GetProperty("attachments")[0];

        Assert.Equal("report.pdf", attachment.GetProperty("filename").GetString());
        Assert.Equal("application/pdf", attachment.GetProperty("type").GetString());
        Assert.Equal(contentBase64, attachment.GetProperty("content").GetString());
    }
}
