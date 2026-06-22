using System.ComponentModel.DataAnnotations;

namespace EmailConsumerService.Contracts.V1;

public class SendEmailRequest
{
    public int EmailLogId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one recipient is required.")]
    public List<string> To { get; set; } = [];

    [EmailAddress]
    public string? From { get; set; }

    public string? FromName { get; set; }

    public List<string> Cc { get; set; } = [];

    public List<string> Bcc { get; set; } = [];

    [Required]
    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; }

    public List<EmailAttachmentRequest> Attachments { get; set; } = [];
}
