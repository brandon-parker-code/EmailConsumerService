using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmailConsumerService.HealthChecks;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
