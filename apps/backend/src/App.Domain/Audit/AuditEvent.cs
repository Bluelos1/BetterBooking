namespace App.Domain.Audit;

public sealed class AuditEvent
{
    private AuditEvent()
    {
        EventType = string.Empty;
        SubjectType = string.Empty;
    }

    private AuditEvent(
        Guid id,
        string eventType,
        Guid? actorUserId,
        string subjectType,
        Guid? subjectId,
        DateTimeOffset createdAt)
    {
        Id = id;
        EventType = eventType;
        ActorUserId = actorUserId;
        SubjectType = subjectType;
        SubjectId = subjectId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string SubjectType { get; private set; }

    public Guid? SubjectId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditEvent Create(
        Guid id,
        string eventType,
        Guid? actorUserId,
        string subjectType,
        Guid? subjectId,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Audit event id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Audit event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(subjectType))
        {
            throw new ArgumentException("Audit subject type is required.", nameof(subjectType));
        }

        return new AuditEvent(
            id,
            eventType.Trim(),
            actorUserId,
            subjectType.Trim(),
            subjectId,
            createdAt);
    }
}
