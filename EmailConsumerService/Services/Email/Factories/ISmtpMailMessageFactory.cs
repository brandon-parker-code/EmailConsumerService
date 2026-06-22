using System.Net.Mail;
using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Email.Factories;

public interface ISmtpMailMessageFactory
{
    MailMessage Create(EmailMessage message, string fromEmail, string fromName);
}
