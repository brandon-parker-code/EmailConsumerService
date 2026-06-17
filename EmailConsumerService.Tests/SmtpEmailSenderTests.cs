using EmailConsumerService.Configuration;
using EmailConsumerService.Models;
using EmailConsumerService.Services.Email;
using EmailConsumerService.Services.Email.Builders;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailConsumerService.Tests;

public class SmtpEmailSenderTests
{
    private readonly Mock<ILogger<SmtpEmailSender>> _logger = new();
    private readonly SmtpMailMessageFactory _mailMessageFactory = new();

    private static SmtpOptions CreateOptions() => new()
    {
        Host = "smtp.example.com",
        Port = 587,
        DefaultFromEmail = "noreply@example.com",
        DefaultFromName = "Example Sender"
    };

    private static EmailMessage CreateValidMessage() => new()
    {
        To = ["recipient@example.com"],
        Subject = "Test subject",
        Body = "Test body"
    };

    private SmtpEmailSender CreateSender() =>
        new(CreateOptions(), _mailMessageFactory, _logger.Object);

    [Fact]
    public async Task SendAsync_WhenHostMissing_ThrowsInvalidOperationException()
    {
        var sender = new SmtpEmailSender(new SmtpOptions(), _mailMessageFactory, _logger.Object);
        var message = CreateValidMessage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(message));

        Assert.Equal("Smtp:Host must be configured.", exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenFromMissingAndNoDefault_ThrowsInvalidOperationException()
    {
        var sender = new SmtpEmailSender(
            new SmtpOptions { Host = "smtp.example.com" },
            _mailMessageFactory,
            _logger.Object);
        var message = CreateValidMessage();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(message));

        Assert.Contains("Smtp:DefaultFromEmail must be configured", exception.Message);
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
    public async Task SendAsync_WhenAttachmentHasInvalidBase64_ThrowsFormatException()
    {
        var sender = CreateSender();
        var message = CreateValidMessage();
        message.Attachments.Add(new EmailAttachment
        {
            FileName = "file.txt",
            ContentBase64 = "not-valid-base64!!!"
        });

        await Assert.ThrowsAsync<FormatException>(() => sender.SendAsync(message));
    }
}
