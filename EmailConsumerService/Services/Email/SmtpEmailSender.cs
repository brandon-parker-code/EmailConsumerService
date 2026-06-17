using System.Net;
using System.Net.Mail;
using EmailConsumerService.Configuration;
using EmailConsumerService.Models;
using EmailConsumerService.Services.Email.Builders;

namespace EmailConsumerService.Services.Email;

public class SmtpEmailSender(
    SmtpOptions options,
    ISmtpMailMessageFactory mailMessageFactory,
    ILogger<SmtpEmailSender> logger) : ISmtpEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new InvalidOperationException("Smtp:Host must be configured.");
        }

        var fromEmail = string.IsNullOrWhiteSpace(message.From)
            ? options.DefaultFromEmail
            : message.From;

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException(
                "Email message must include a From address or Smtp:DefaultFromEmail must be configured.");
        }

        var fromName = string.IsNullOrWhiteSpace(message.FromName)
            ? options.DefaultFromName
            : message.FromName;

        using var mailMessage = mailMessageFactory.Create(message, fromEmail, fromName);
        using var smtpClient = CreateSmtpClient();

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);

        logger.LogInformation(
            "Successfully sent email via SMTP to {Recipients} with subject {Subject}.",
            string.Join(", ", message.To),
            message.Subject);
    }

    private SmtpClient CreateSmtpClient()
    {
        var smtpClient = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            smtpClient.Credentials = new NetworkCredential(options.Username, options.Password);
        }

        return smtpClient;
    }
}
