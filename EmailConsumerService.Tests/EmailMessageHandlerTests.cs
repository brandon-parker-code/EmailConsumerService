using EmailConsumerService.Models;
using EmailConsumerService.Services.Email;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailConsumerService.Tests;

public class EmailMessageHandlerTests
{
    private readonly Mock<ISendGridEmailSender> _sendGridEmailSender = new();
    private readonly Mock<ISmtpEmailSender> _smtpEmailSender = new();
    private readonly Mock<ILogger<EmailMessageHandler>> _logger = new();

    private EmailMessageHandler CreateHandler() =>
        new(_sendGridEmailSender.Object, _smtpEmailSender.Object, _logger.Object);

    private static EmailMessage CreateValidMessage() => new()
    {
        To = ["recipient@example.com"],
        Subject = "Test subject",
        Body = "Test body"
    };

    [Fact]
    public async Task HandleAsync_WhenNoRecipients_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();
        message.To.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(message));

        Assert.Equal("Email message must include at least one recipient.", exception.Message);
        _sendGridEmailSender.Verify(
            sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSubjectMissing_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();
        message.Subject = "   ";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(message));

        Assert.Equal("Email message must include a subject.", exception.Message);
        _sendGridEmailSender.Verify(
            sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSendGridSucceeds_UsesSendGridOnly()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();
        var cancellationToken = new CancellationTokenSource().Token;

        await handler.HandleAsync(message, cancellationToken);

        _sendGridEmailSender.Verify(
            sender => sender.SendAsync(message, cancellationToken),
            Times.Once);
        _smtpEmailSender.Verify(
            sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSendGridFails_FallsBackToSmtp()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();
        var cancellationToken = new CancellationTokenSource().Token;
        var sendGridException = new InvalidOperationException("SendGrid unavailable");

        _sendGridEmailSender
            .Setup(sender => sender.SendAsync(message, cancellationToken))
            .ThrowsAsync(sendGridException);

        await handler.HandleAsync(message, cancellationToken);

        _sendGridEmailSender.Verify(
            sender => sender.SendAsync(message, cancellationToken),
            Times.Once);
        _smtpEmailSender.Verify(
            sender => sender.SendAsync(message, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSendGridAndSmtpFail_ThrowsSmtpException()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();
        var smtpException = new InvalidOperationException("SMTP unavailable");

        _sendGridEmailSender
            .Setup(sender => sender.SendAsync(message, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SendGrid unavailable"));
        _smtpEmailSender
            .Setup(sender => sender.SendAsync(message, It.IsAny<CancellationToken>()))
            .ThrowsAsync(smtpException);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(message));

        Assert.Same(smtpException, exception);
    }
}
