using EmailConsumerService.Configuration;
using EmailConsumerService.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmailConsumerService.Tests;

public class DatabaseHealthCheckTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CheckHealthAsync_WhenConnectionStringMissing_ReturnsUnhealthy(string connectionString)
    {
        var healthCheck = new DatabaseHealthCheck(new DatabaseOptions { ConnectionString = connectionString });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not configured", result.Description);
    }
}
