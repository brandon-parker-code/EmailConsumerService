using EmailConsumerService.Models;
using EmailConsumerService.Services.Email.Builders;

namespace EmailConsumerService.Tests;

public class SmtpMailMessageFactoryTests
{
    private readonly SmtpMailMessageFactory _factory = new();

    private static EmailMessage CreateValidMessage() => new()
    {
        To = ["recipient@example.com"],
        Subject = "Test subject",
        Body = "Test body"
    };

    [Fact]
    public void Create_WhenMessageHasAttachment_AddsAttachmentWithDecodedContent()
    {
        var message = CreateValidMessage();
        var contentBytes = "hello"u8.ToArray();
        message.Attachments.Add(new EmailAttachment
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ContentBase64 = Convert.ToBase64String(contentBytes)
        });

        using var mailMessage = _factory.Create(message, "from@example.com", "Sender");

        var attachment = Assert.Single(mailMessage.Attachments);
        Assert.Equal("report.pdf", attachment.Name);
        Assert.Equal("application/pdf", attachment.ContentType.MediaType);

        using var stream = new MemoryStream();
        attachment.ContentStream.CopyTo(stream);
        Assert.Equal(contentBytes, stream.ToArray());
    }

    [Fact]
    public void Create_WhenAttachmentHasInvalidBase64_ThrowsFormatException()
    {
        var message = CreateValidMessage();
        message.Attachments.Add(new EmailAttachment
        {
            FileName = "file.txt",
            ContentBase64 = "not-valid-base64!!!"
        });

        Assert.Throws<FormatException>(() =>
            _factory.Create(message, "from@example.com", "Sender"));
    }

    [Fact]
    public void Create_WhenAttachmentMissingFileName_ThrowsInvalidOperationException()
    {
        var message = CreateValidMessage();
        message.Attachments.Add(new EmailAttachment
        {
            FileName = "",
            ContentBase64 = Convert.ToBase64String("content"u8.ToArray())
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _factory.Create(message, "from@example.com", "Sender"));

        Assert.Equal("Each attachment must include a file name.", exception.Message);
    }

    [Fact]
    public void Create_WhenAttachmentMissingContent_ThrowsInvalidOperationException()
    {
        var message = CreateValidMessage();
        message.Attachments.Add(new EmailAttachment
        {
            FileName = "file.txt",
            ContentBase64 = ""
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _factory.Create(message, "from@example.com", "Sender"));

        Assert.Equal("Attachment 'file.txt' must include base64 content.", exception.Message);
    }
}
