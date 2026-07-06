using App.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace App.IntegrationTests.TestSupport;

public sealed class PostgreSqlFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;

    private PostgreSqlFixture(PostgreSqlContainer container)
    {
        _container = container;
    }

    public static async Task<PostgreSqlFixture> StartAsync()
    {
        var container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("betterbooking_test")
            .WithUsername("betterbooking_test")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();

        var fixture = new PostgreSqlFixture(container);
        await fixture.InitializeAsync();

        return fixture;
    }

    private async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var dbContext = CreateDbContext();

        await dbContext.AuditEvents.ExecuteDeleteAsync();
        await dbContext.Reservations.ExecuteDeleteAsync();
        await dbContext.Listings.ExecuteDeleteAsync();
        await dbContext.Users.ExecuteDeleteAsync();
    }
}
