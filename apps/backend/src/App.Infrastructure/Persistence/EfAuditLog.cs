using App.Application.Audit;
using App.Application.Common;
using App.Domain.Audit;

namespace App.Infrastructure.Persistence;

public sealed class EfAuditLog : IAuditLog
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISystemClock _clock;

    public EfAuditLog(ApplicationDbContext dbContext, ISystemClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(),
            entry.EventType,
            entry.ActorUserId,
            entry.SubjectType,
            entry.SubjectId,
            _clock.UtcNow);

        await _dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }
}
