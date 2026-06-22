using EmailConsumerService.Configuration;
using EmailConsumerService.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EmailConsumerService.Services.Email;

public class SendGridEmailSender(
    SendGridOptions options,
    SendGridClient sendGridClient,
    ILogger<SendGridEmailSender> logger) : ISendGridEmailSender
{
    public async Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var fromEmail = string.IsNullOrWhiteSpace(message.From)
            ? options.DefaultFromEmail
            : message.From;

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException(
                "Email message must include a From address or SendGrid:DefaultFromEmail must be configured.");
        }

        var fromName = string.IsNullOrWhiteSpace(message.FromName)
            ? options.DefaultFromName
            : message.FromName;

        var sendGridMessage = new SendGridMessage
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = message.Subject
        };

        sendGridMessage.AddTos(message.To.Select(address => new EmailAddress(address)).ToList());

        if (message.Cc.Count > 0)
        {
            sendGridMessage.AddCcs(message.Cc.Select(address => new EmailAddress(address)).ToList());
        }

        if (message.Bcc.Count > 0)
        {
            sendGridMessage.AddBccs(message.Bcc.Select(address => new EmailAddress(address)).ToList());
        }

        sendGridMessage.AddContent(
            message.IsHtml ? MimeType.Html : MimeType.Text,
            message.Body);

        foreach (var attachment in message.Attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.FileName))
            {
                throw new InvalidOperationException("Each attachment must include a file name.");
            }

            if (string.IsNullOrWhiteSpace(attachment.ContentBase64))
            {
                throw new InvalidOperationException(
                    $"Attachment '{attachment.FileName}' must include base64 content.");
            }

            sendGridMessage.AddAttachment(
                attachment.FileName,
                attachment.ContentBase64,
                attachment.ContentType);
        }

        var response = await sendGridClient.SendEmailAsync(sendGridMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "SendGrid returned {StatusCode} for email to {Recipients}: {ResponseBody}",
                response.StatusCode,
                string.Join(", ", message.To),
                responseBody);

            throw new InvalidOperationException(
                $"SendGrid request failed with status {(int)response.StatusCode}.");
        }

        var sendGridMessageId = ExtractMessageId(response);

        logger.LogInformation(
            "Successfully sent email to {Recipients} with subject {Subject}. SendGrid message id {SendGridMessageId}.",
            string.Join(", ", message.To),
            message.Subject,
            sendGridMessageId);

        return sendGridMessageId;
    }

    private static string? ExtractMessageId(SendGrid.Response response) =>
        response.Headers.TryGetValues("X-Message-Id", out var values)
            ? values.FirstOrDefault()
            : null;
}
