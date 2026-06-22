using System.Text.Json;
using EmailConsumerService.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmailConsumerService.Tests;

public class HealthCheckResponseWriterTests
{
    private static async Task<(string Body, string? ContentType)> WriteAsync(HealthReport report)
    {
        var context = new DefaultHttpContext();
        using var stream = new MemoryStream();
        context.Response.Body = stream;

        await HealthCheckResponseWriter.WriteJsonAsync(context, report);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var body = await reader.ReadToEndAsync();
        return (body, context.Response.ContentType);
    }

    private static HealthReport CreateReport() => new(
        new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new(HealthStatus.Healthy, description: null, TimeSpan.FromMilliseconds(8), exception: null, data: null),
            ["kafka"] = new(HealthStatus.Unhealthy, "Unable to reach the Kafka cluster.", TimeSpan.FromMilliseconds(5), exception: null, data: null)
        },
        TimeSpan.FromMilliseconds(13));

    [Fact]
    public async Task WriteJsonAsync_SetsJsonContentType()
    {
        var (_, contentType) = await WriteAsync(CreateReport());

        Assert.Equal("application/json", contentType);
    }

    [Fact]
    public async Task WriteJsonAsync_WritesAggregateStatusAndPerCheckEntries()
    {
        var (body, _) = await WriteAsync(CreateReport());

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal("Unhealthy", root.GetProperty("status").GetString());

        var checks = root.GetProperty("checks").EnumerateArray().ToList();
        Assert.Equal(2, checks.Count);

        var kafka = checks.Single(check => check.GetProperty("name").GetString() == "kafka");
        Assert.Equal("Unhealthy", kafka.GetProperty("status").GetString());
        Assert.Equal("Unable to reach the Kafka cluster.", kafka.GetProperty("description").GetString());

        var database = checks.Single(check => check.GetProperty("name").GetString() == "database");
        Assert.Equal("Healthy", database.GetProperty("status").GetString());
    }
}
