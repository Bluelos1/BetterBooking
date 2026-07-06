using App.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");

        builder.HasKey(auditEvent => auditEvent.Id);

        builder.Property(auditEvent => auditEvent.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(auditEvent => auditEvent.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditEvent => auditEvent.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(auditEvent => auditEvent.SubjectType)
            .HasColumnName("subject_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditEvent => auditEvent.SubjectId)
            .HasColumnName("subject_id");

        builder.Property(auditEvent => auditEvent.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(auditEvent => auditEvent.CreatedAt)
            .HasDatabaseName("ix_audit_events_created_at");

        builder.HasIndex(auditEvent => new { auditEvent.ActorUserId, auditEvent.CreatedAt })
            .HasDatabaseName("ix_audit_events_actor_user_id_created_at");
    }
}
