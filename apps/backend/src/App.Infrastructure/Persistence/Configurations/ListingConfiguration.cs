using App.Domain.Listings;
using App.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations;

internal sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("listings");

        builder.HasKey(listing => listing.Id);

        builder.Property(listing => listing.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(listing => listing.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(listing => listing.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(listing => listing.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(listing => listing.Location)
            .HasColumnName("location")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(listing => listing.NightlyPriceAmount)
            .HasColumnName("nightly_price_amount")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(listing => listing.MaxGuests)
            .HasColumnName("max_guests")
            .IsRequired();

        builder.Property(listing => listing.BedroomCount)
            .HasColumnName("bedroom_count")
            .IsRequired();

        builder.Property(listing => listing.BathroomCount)
            .HasColumnName("bathroom_count")
            .IsRequired();

        builder.Property(listing => listing.HeroImageUrl)
            .HasColumnName("hero_image_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(listing => listing.Amenities)
            .HasColumnName("amenities")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(listing => listing.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(listing => listing.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(listing => listing.Status)
            .HasDatabaseName("ix_listings_status");

        builder.HasIndex(listing => listing.Location)
            .HasDatabaseName("ix_listings_location");

        builder.HasIndex(listing => new { listing.OwnerUserId, listing.Status })
            .HasDatabaseName("ix_listings_owner_user_id_status");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(listing => listing.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_listings_users_owner_user_id");
    }
}
