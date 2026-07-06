namespace App.Application.Audit;

public sealed record AuditLogEntry(
    string EventType,
    Guid? ActorUserId,
    string SubjectType,
    Guid? SubjectId);
