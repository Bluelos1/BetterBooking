using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ListingDetailsAndReservationPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "reservations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Unpaid");

            migrationBuilder.AddColumn<string>(
                name: "amenities",
                table: "listings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "bathroom_count",
                table: "listings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "bedroom_count",
                table: "listings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "listings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "A comfortable stay with the essentials configured for local development.");

            migrationBuilder.AddColumn<string>(
                name: "hero_image_url",
                table: "listings",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "listings",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "Local Test District");

            migrationBuilder.AddColumn<int>(
                name: "max_guests",
                table: "listings",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "nightly_price_amount",
                table: "listings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 120m);

            migrationBuilder.CreateIndex(
                name: "ix_listings_location",
                table: "listings",
                column: "location");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_listings_location",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "amenities",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "bathroom_count",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "bedroom_count",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "description",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "hero_image_url",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "location",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "max_guests",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "nightly_price_amount",
                table: "listings");
        }
    }
}
