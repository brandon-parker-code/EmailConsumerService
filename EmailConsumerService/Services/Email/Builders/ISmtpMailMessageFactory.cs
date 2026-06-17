using System.Net.Mail;
using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Email.Builders;

public interface ISmtpMailMessageFactory
{
    MailMessage Create(EmailMessage message, string fromEmail, string fromName);
}
