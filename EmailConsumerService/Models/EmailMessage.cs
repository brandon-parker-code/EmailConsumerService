namespace EmailConsumerService.Models;

public class EmailMessage
{
    public List<string> To { get; set; } = [];

    public string? From { get; set; }

    public string? FromName { get; set; }

    public List<string> Cc { get; set; } = [];

    public List<string> Bcc { get; set; } = [];

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; }

    public List<EmailAttachment> Attachments { get; set; } = [];
}
