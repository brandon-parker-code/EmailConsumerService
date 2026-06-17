namespace EmailConsumerService.Models;

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public string ContentBase64 { get; set; } = string.Empty;
}
