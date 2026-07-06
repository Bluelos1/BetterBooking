using App.Domain.Listings;
using App.Domain.Reservations;
using App.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations", table =>
        {
            table.HasCheckConstraint("ck_reservations_period_valid", "end_date > start_date");
        });

        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(reservation => reservation.ListingId)
            .HasColumnName("listing_id")
            .IsRequired();

        builder.Property(reservation => reservation.GuestUserId)
            .HasColumnName("guest_user_id")
            .IsRequired();

        builder.ComplexProperty(reservation => reservation.Period, periodBuilder =>
        {
            periodBuilder.Property(period => period.StartDate)
                .HasColumnName("start_date")
                .HasColumnType("date")
                .IsRequired();

            periodBuilder.Property(period => period.EndDate)
                .HasColumnName("end_date")
                .HasColumnType("date")
                .IsRequired();
        });

        builder.Property(reservation => reservation.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(reservation => reservation.PaymentStatus)
            .HasColumnName("payment_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(reservation => reservation.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(reservation => reservation.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(reservation => new { reservation.ListingId, reservation.Status })
            .HasDatabaseName("ix_reservations_listing_id_status");

        builder.HasIndex(reservation => new { reservation.GuestUserId, reservation.CreatedAt })
            .HasDatabaseName("ix_reservations_guest_user_id_created_at");

        builder.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(reservation => reservation.ListingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_reservations_listings_listing_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(reservation => reservation.GuestUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_reservations_users_guest_user_id");
    }
}
