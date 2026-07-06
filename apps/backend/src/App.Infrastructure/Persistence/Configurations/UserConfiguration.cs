using App.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(user => user.ExternalProvider)
            .HasColumnName("external_provider")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.ExternalSubject)
            .HasColumnName("external_subject")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(320);

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200);

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(user => new { user.ExternalProvider, user.ExternalSubject })
            .IsUnique()
            .HasDatabaseName("ux_users_external_provider_external_subject");
    }
}
