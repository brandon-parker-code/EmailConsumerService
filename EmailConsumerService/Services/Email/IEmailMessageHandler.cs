using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Email;

public interface IEmailMessageHandler
{
    Task HandleAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
