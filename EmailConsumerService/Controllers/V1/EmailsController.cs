using Asp.Versioning;
using EmailConsumerService.Contracts.V1;
using EmailConsumerService.Models;
using EmailConsumerService.Services.Kafka;
using Microsoft.AspNetCore.Mvc;

namespace EmailConsumerService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/emails")]
[Produces("application/json")]
public class EmailsController(IKafkaEmailProducer emailProducer) : ControllerBase
{
    /// <summary>
    /// Queues an email message for delivery by publishing it to the Kafka topic.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SendEmailResponse>> SendEmail(
        [FromBody] SendEmailRequest request,
        CancellationToken cancellationToken)
    {
        var message = new EmailMessage
        {
            To = request.To,
            From = request.From,
            FromName = request.FromName,
            Cc = request.Cc,
            Bcc = request.Bcc,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml,
            Attachments = request.Attachments
                .Select(attachment => new EmailAttachment
                {
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    ContentBase64 = attachment.ContentBase64
                })
                .ToList()
        };

        var messageId = await emailProducer.ProduceAsync(message, cancellationToken);

        return Accepted(new SendEmailResponse { MessageId = messageId });
    }
}
