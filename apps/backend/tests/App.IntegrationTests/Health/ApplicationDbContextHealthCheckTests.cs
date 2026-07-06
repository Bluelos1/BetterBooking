using App.Infrastructure.Health;
using App.IntegrationTests.TestSupport;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace App.IntegrationTests.Health;

public sealed class ApplicationDbContextHealthCheckTests
{
    [DockerAvailableFact]
    public async Task CheckHealthAsync_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await using var dbContext = fixture.CreateDbContext();
        var healthCheck = new ApplicationDbContextHealthCheck(dbContext);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
