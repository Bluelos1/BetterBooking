using App.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace App.Infrastructure.Health;

public sealed class ApplicationDbContextHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicationDbContextHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Application database is reachable.")
                : HealthCheckResult.Unhealthy("Application database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Application database health check failed.", exception);
        }
    }
}
