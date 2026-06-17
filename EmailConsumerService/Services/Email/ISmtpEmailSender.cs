using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Email;

public interface ISmtpEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
