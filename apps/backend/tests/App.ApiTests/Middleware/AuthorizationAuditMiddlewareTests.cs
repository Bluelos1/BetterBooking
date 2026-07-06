using App.Api.Middleware;
using App.Application.Audit;
using App.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace App.ApiTests.Middleware;

public sealed class AuthorizationAuditMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenResponseIsUnauthorized_WritesAuditEvent()
    {
        var auditLog = new FakeAuditLog();
        var unitOfWork = new FakeUnitOfWork();
        var middleware = new AuthorizationAuditMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        });
        var context = CreateHttpContext(auditLog, unitOfWork);

        await middleware.InvokeAsync(context);

        var auditEvent = Assert.Single(auditLog.Entries);
        Assert.Equal(AuditEventTypes.AuthorizationFailed, auditEvent.EventType);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseIsSuccessful_DoesNotWriteAuditEvent()
    {
        var auditLog = new FakeAuditLog();
        var unitOfWork = new FakeUnitOfWork();
        var middleware = new AuthorizationAuditMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;

            return Task.CompletedTask;
        });
        var context = CreateHttpContext(auditLog, unitOfWork);

        await middleware.InvokeAsync(context);

        Assert.Empty(auditLog.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static DefaultHttpContext CreateHttpContext(FakeAuditLog auditLog, FakeUnitOfWork unitOfWork)
    {
        var services = new ServiceCollection()
            .AddSingleton<IAuditLog>(auditLog)
            .AddSingleton<IApplicationUnitOfWork>(unitOfWork)
            .BuildServiceProvider();

        return new DefaultHttpContext
        {
            RequestServices = services
        };
    }

    private sealed class FakeAuditLog : IAuditLog
    {
        public List<AuditLogEntry> Entries { get; } = [];

        public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IApplicationUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            return operation(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;

            return Task.CompletedTask;
        }
    }
}
