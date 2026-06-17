using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Email;

public interface ISendGridEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
