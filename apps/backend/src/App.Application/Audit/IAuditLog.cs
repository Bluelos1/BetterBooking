namespace App.Application.Audit;

public interface IAuditLog
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken);
}
