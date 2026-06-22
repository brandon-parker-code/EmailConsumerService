using EmailConsumerService.Models;
using EmailConsumerService.Repositories;

namespace EmailConsumerService.Services.Email;

public class EmailMessageHandler(
    ISendGridEmailSender sendGridEmailSender,
    ISmtpEmailSender smtpEmailSender,
    IEmailLogRepository emailLogRepository,
    ILogger<EmailMessageHandler> logger) : IEmailMessageHandler
{
    public async Task HandleAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message.To.Count == 0)
        {
            throw new InvalidOperationException("Email message must include at least one recipient.");
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            throw new InvalidOperationException("Email message must include a subject.");
        }

        logger.LogInformation(
            "Processing email to {Recipients} with subject {Subject} and {AttachmentCount} attachment(s).",
            string.Join(", ", message.To),
            message.Subject,
            message.Attachments.Count);

        string? sendGridMessageId = null;
        var sentViaSendGrid = false;

        try
        {
            sendGridMessageId = await sendGridEmailSender.SendAsync(message, cancellationToken);
            sentViaSendGrid = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "SendGrid failed for email to {Recipients} with subject {Subject}. Falling back to SMTP.",
                string.Join(", ", message.To),
                message.Subject);

            await smtpEmailSender.SendAsync(message, cancellationToken);
        }

        if (sentViaSendGrid)
        {
            await UpdateEmailLogAsync(message, sendGridMessageId, cancellationToken);
        }
    }

    private async Task UpdateEmailLogAsync(
        EmailMessage message,
        string? sendGridMessageId,
        CancellationToken cancellationToken)
    {
        if (message.EmailLogId <= 0 || string.IsNullOrWhiteSpace(sendGridMessageId))
        {
            return;
        }

        await emailLogRepository.UpdateSendGridIdAsync(
            message.EmailLogId,
            sendGridMessageId,
            cancellationToken);

        logger.LogInformation(
            "Updated tblEmailLog {EmailLogId} with SendGrid message id {SendGridMessageId}.",
            message.EmailLogId,
            sendGridMessageId);
    }
}
