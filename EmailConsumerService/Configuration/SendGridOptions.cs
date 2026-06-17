namespace EmailConsumerService.Configuration;

public class SendGridOptions
{
    public const string SectionName = "SendGrid";

    public string ApiKey { get; set; } = string.Empty;

    public string DefaultFromEmail { get; set; } = string.Empty;

    public string DefaultFromName { get; set; } = string.Empty;
}
