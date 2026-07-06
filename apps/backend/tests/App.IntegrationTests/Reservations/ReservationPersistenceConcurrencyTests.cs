using App.Application.Common;
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

public sealed class ReservationPersistenceConcurrencyTests
{
    [DockerAvailableFact]
    public async Task DatabaseConstraint_WhenOverlappingActiveReservationsAreInserted_RejectsSecondReservation()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var ownerUserId = await CreateUserAsync(fixture, "owner-manual");
        var guestUserId = await CreateUserAsync(fixture, "guest-manual");
        var listingId = await CreatePublishedListingAsync(fixture, ownerUserId);
        var period = DateRange.Create(new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 14));

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Reservations.Add(Reservation.CreatePending(
                Guid.NewGuid(),
                listingId,
                guestUserId,
                period,
                DateTimeOffset.UtcNow));

            await dbContext.SaveChangesAsync();
        }

        await using var secondContext = fixture.CreateDbContext();
        secondContext.Reservations.Add(Reservation.CreatePending(
            Guid.NewGuid(),
            listingId,
            guestUserId,
            period,
            DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => secondContext.SaveChangesAsync());

        Assert.Contains("ex_reservations_no_active_overlap", exception.InnerException?.Message ?? exception.Message, StringComparison.Ordinal);
    }

    [DockerAvailableFact]
    public async Task CreateReservationHandler_WhenOverlappingRequestsRunConcurrently_PersistsOnlyOneReservation()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var ownerUserId = await CreateUserAsync(fixture, "owner-concurrent");
        var firstGuestUserId = await CreateUserAsync(fixture, "guest-concurrent-1");
        var secondGuestUserId = await CreateUserAsync(fixture, "guest-concurrent-2");
        var listingId = await CreatePublishedListingAsync(fixture, ownerUserId);
        var startDate = new DateOnly(2027, 7, 10);
        var endDate = new DateOnly(2027, 7, 14);
        using var barrier = new Barrier(2);

        var attempts = new[]
        {
            RunCreateReservationAttemptAsync(fixture, listingId, firstGuestUserId, startDate, endDate, barrier),
            RunCreateReservationAttemptAsync(fixture, listingId, secondGuestUserId, startDate, endDate, barrier)
        };

        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.Equal(1, results.Count(result => result.Error == CreateReservationError.ListingUnavailable));

        await using var verificationContext = fixture.CreateDbContext();
        var persistedReservations = await verificationContext.Reservations
            .Where(reservation => reservation.ListingId == listingId)
            .ToListAsync();

        Assert.Single(persistedReservations);
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

    private static async Task<Guid> CreatePublishedListingAsync(PostgreSqlFixture fixture, Guid ownerUserId)
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "Concurrency test listing", DateTimeOffset.UtcNow);
        listing.Publish();

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        return listing.Id;
    }

    private static async Task<CreateReservationResult> RunCreateReservationAttemptAsync(
        PostgreSqlFixture fixture,
        Guid listingId,
        Guid guestUserId,
        DateOnly startDate,
        DateOnly endDate,
        Barrier barrier)
    {
        return await Task.Run(async () =>
        {
            await using var dbContext = fixture.CreateDbContext();
            var handler = new CreateReservationHandler(
                new EfListingRepository(dbContext),
                new EfReservationRepository(dbContext),
                new EfApplicationUnitOfWork(dbContext),
                new EfAuditLog(dbContext, new FixedClock(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero))),
                new FixedClock(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero)));

            barrier.SignalAndWait(TimeSpan.FromSeconds(30));

            return await handler.HandleAsync(new CreateReservationCommand(
                Guid.NewGuid(),
                listingId,
                guestUserId,
                startDate,
                endDate), CancellationToken.None);
        });
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
