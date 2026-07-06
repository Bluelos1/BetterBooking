using App.Application.Audit;
using App.Application.Common;
using App.Application.Listings;
using App.Application.Reservations;
using App.Domain.Availability;
using App.Domain.Listings;
using App.Domain.Reservations;
using App.Domain.Users;
using App.Infrastructure.Persistence;
using App.Infrastructure.Persistence.Repositories;
using App.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace App.IntegrationTests.Reservations;

public sealed class ReservationCancellationTests
{
    [DockerAvailableFact]
    public async Task CancelReservationHandler_WhenGuestCancels_FreesListingAvailability()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var ownerUserId = await CreateUserAsync(fixture, "owner-cancel");
        var guestUserId = await CreateUserAsync(fixture, "guest-cancel");
        var listingId = await CreateListingAsync(fixture, ownerUserId);
        var reservationId = await CreateReservationAsync(fixture, listingId, guestUserId);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var handler = CreateCancelReservationHandler(dbContext);

            var result = await handler.HandleAsync(new CancelReservationCommand(reservationId, guestUserId), CancellationToken.None);

            Assert.True(result.Succeeded);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var availabilityHandler = new CheckListingAvailabilityHandler(
            new EfListingRepository(verificationContext),
            new EfReservationRepository(verificationContext));
        var availabilityResult = await availabilityHandler.HandleAsync(new CheckListingAvailabilityQuery(
            listingId,
            new DateOnly(2027, 7, 10),
            new DateOnly(2027, 7, 12)), CancellationToken.None);
        var persistedReservation = await verificationContext.Reservations.FirstAsync(reservation => reservation.Id == reservationId);
        var auditEventExists = await verificationContext.AuditEvents.AnyAsync(auditEvent =>
            auditEvent.EventType == AuditEventTypes.ReservationCancelled &&
            auditEvent.SubjectId == reservationId);

        Assert.Equal(ReservationStatus.Cancelled, persistedReservation.Status);
        Assert.True(availabilityResult.Succeeded);
        Assert.True(availabilityResult.Available);
        Assert.True(auditEventExists);
    }

    [DockerAvailableFact]
    public async Task CancelReservationHandler_WhenCallerIsNotGuest_DoesNotCancel()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var ownerUserId = await CreateUserAsync(fixture, "owner-cancel-forbidden");
        var guestUserId = await CreateUserAsync(fixture, "guest-cancel-forbidden");
        var otherGuestUserId = await CreateUserAsync(fixture, "other-guest-cancel-forbidden");
        var listingId = await CreateListingAsync(fixture, ownerUserId);
        var reservationId = await CreateReservationAsync(fixture, listingId, guestUserId);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var handler = CreateCancelReservationHandler(dbContext);

            var result = await handler.HandleAsync(new CancelReservationCommand(reservationId, otherGuestUserId), CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(CancelReservationError.Forbidden, result.Error);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var persistedReservation = await verificationContext.Reservations.FirstAsync(reservation => reservation.Id == reservationId);

        Assert.Equal(ReservationStatus.Pending, persistedReservation.Status);
    }

    private static CancelReservationHandler CreateCancelReservationHandler(ApplicationDbContext dbContext)
    {
        var clock = new FixedClock(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        return new CancelReservationHandler(
            new EfReservationRepository(dbContext),
            new EfApplicationUnitOfWork(dbContext),
            new EfAuditLog(dbContext, clock),
            clock);
    }

    private static async Task<Guid> CreateUserAsync(PostgreSqlFixture fixture, string externalSubject)
    {
        var user = User.Create(
            Guid.NewGuid(),
            "integration-tests",
            externalSubject,
            null,
            null,
            DateTimeOffset.UtcNow);

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user.Id;
    }

    private static async Task<Guid> CreateListingAsync(PostgreSqlFixture fixture, Guid ownerUserId)
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "Cancellation test listing", DateTimeOffset.UtcNow);
        listing.Publish();

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        return listing.Id;
    }

    private static async Task<Guid> CreateReservationAsync(PostgreSqlFixture fixture, Guid listingId, Guid guestUserId)
    {
        var reservation = Reservation.CreatePending(
            Guid.NewGuid(),
            listingId,
            guestUserId,
            DateRange.Create(new DateOnly(2027, 7, 10), new DateOnly(2027, 7, 12)),
            DateTimeOffset.UtcNow);

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync();

        return reservation.Id;
    }

    private sealed class FixedClock : ISystemClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
