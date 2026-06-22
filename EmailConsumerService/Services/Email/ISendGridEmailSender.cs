using EmailConsumerService.Models;

namespace EmailConsumerService.Services.Email;

public interface ISendGridEmailSender
{
    /// <summary>
    /// Sends the email via SendGrid and returns the SendGrid message id
    /// (the <c>X-Message-Id</c> response header), or <c>null</c> when not present.
    /// </summary>
    Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
