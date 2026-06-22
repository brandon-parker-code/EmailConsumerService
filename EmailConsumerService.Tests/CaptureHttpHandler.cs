namespace EmailConsumerService.Tests;

using System.Net;

internal sealed class CaptureHttpHandler : HttpMessageHandler
{
    public string? LastRequestBody { get; private set; }

    public string? MessageId { get; set; } = "sg-message-id";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var response = new HttpResponseMessage(HttpStatusCode.Accepted);

        if (MessageId is not null)
        {
            response.Headers.Add("X-Message-Id", MessageId);
        }

        return response;
    }
}
