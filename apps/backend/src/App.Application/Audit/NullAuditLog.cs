namespace App.Application.Audit;

public sealed class NullAuditLog : IAuditLog
{
    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;
}
