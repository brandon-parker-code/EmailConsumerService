using EmailConsumerService.Models;
using EmailConsumerService.Repositories;
using EmailConsumerService.Services.Email;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailConsumerService.Tests;

public class EmailMessageHandlerTests
{
    private readonly Mock<ISendGridEmailSender> _sendGridEmailSender = new();
    private readonly Mock<ISmtpEmailSender> _smtpEmailSender = new();
    private readonly Mock<IEmailLogRepository> _emailLogRepository = new();
    private readonly Mock<ILogger<EmailMessageHandler>> _logger = new();

    private EmailMessageHandler CreateHandler() =>
        new(_sendGridEmailSender.Object, _smtpEmailSender.Object, _emailLogRepository.Object, _logger.Object);

    private static EmailMessage CreateValidMessage() => new()
    {
        EmailLogId = 42,
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

    [Fact]
    public async Task HandleAsync_WhenSendGridReturnsMessageId_UpdatesEmailLogWithSendGridId()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();
        message.EmailLogId = 99;
        var cancellationToken = new CancellationTokenSource().Token;

        _sendGridEmailSender
            .Setup(sender => sender.SendAsync(message, cancellationToken))
            .ReturnsAsync("sendgrid-id-abc");

        await handler.HandleAsync(message, cancellationToken);

        _emailLogRepository.Verify(
            repository => repository.UpdateSendGridIdAsync(99, "sendgrid-id-abc", cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSendGridReturnsNoMessageId_DoesNotUpdateEmailLog()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();

        _sendGridEmailSender
            .Setup(sender => sender.SendAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        await handler.HandleAsync(message);

        _emailLogRepository.Verify(
            repository => repository.UpdateSendGridIdAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailLogIdNotSet_DoesNotUpdateEmailLog()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();
        message.EmailLogId = 0;

        _sendGridEmailSender
            .Setup(sender => sender.SendAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync("sendgrid-id-abc");

        await handler.HandleAsync(message);

        _emailLogRepository.Verify(
            repository => repository.UpdateSendGridIdAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSendGridFailsAndFallsBackToSmtp_DoesNotUpdateEmailLog()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();

        _sendGridEmailSender
            .Setup(sender => sender.SendAsync(message, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SendGrid unavailable"));

        await handler.HandleAsync(message);

        _smtpEmailSender.Verify(
            sender => sender.SendAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
        _emailLogRepository.Verify(
            repository => repository.UpdateSendGridIdAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
