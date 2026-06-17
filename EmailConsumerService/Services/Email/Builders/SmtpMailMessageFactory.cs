using System.Net.Mail;
using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Email.Builders;

public class SmtpMailMessageFactory : ISmtpMailMessageFactory
{
    public MailMessage Create(EmailMessage message, string fromEmail, string fromName)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml
        };

        foreach (var recipient in message.To)
        {
            mailMessage.To.Add(recipient);
        }

        foreach (var recipient in message.Cc)
        {
            mailMessage.CC.Add(recipient);
        }

        foreach (var recipient in message.Bcc)
        {
            mailMessage.Bcc.Add(recipient);
        }

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

            var content = Convert.FromBase64String(attachment.ContentBase64);
            var stream = new MemoryStream(content);
            mailMessage.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
        }

        return mailMessage;
    }
}
