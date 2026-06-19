using System.ComponentModel.DataAnnotations;

namespace EmailConsumerService.Contracts.V1;

public class EmailAttachmentRequest
{
    [Required]
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    [Required]
    public string ContentBase64 { get; set; } = string.Empty;
}
