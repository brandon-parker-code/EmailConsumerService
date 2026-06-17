using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Email;

public class EmailMessageHandler(
    ISendGridEmailSender sendGridEmailSender,
    ISmtpEmailSender smtpEmailSender,
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

        try
        {
            await sendGridEmailSender.SendAsync(message, cancellationToken);
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
    }
}
