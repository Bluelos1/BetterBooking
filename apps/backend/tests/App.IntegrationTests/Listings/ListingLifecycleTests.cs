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

namespace App.IntegrationTests.Listings;

public sealed class ListingLifecycleTests
{
    [DockerAvailableFact]
    public async Task ArchiveListingHandler_RemovesListingFromPublicVisibilityAndRejectsNewReservations()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var ownerUserId = await CreateUserAsync(fixture, "owner-archive");
        var guestUserId = await CreateUserAsync(fixture, "guest-archive");
        var nextGuestUserId = await CreateUserAsync(fixture, "guest-archive-next");
        var listingId = await CreateListingAsync(fixture, ownerUserId, "Lifecycle archive listing");
        var existingReservationId = await CreateReservationAsync(
            fixture,
            listingId,
            guestUserId,
            new DateOnly(2027, 8, 10),
            new DateOnly(2027, 8, 12));

        await using (var dbContext = fixture.CreateDbContext())
        {
            var handler = new ArchiveListingHandler(
                new EfListingRepository(dbContext),
                new EfApplicationUnitOfWork(dbContext),
                new EfAuditLog(dbContext, new FixedClock(DateTimeOffset.UtcNow)));

            var archiveResult = await handler.HandleAsync(new UpdateListingStatusCommand(listingId, ownerUserId), CancellationToken.None);

            Assert.True(archiveResult.Succeeded);
        }

        await using var verificationContext = fixture.CreateDbContext();
        var listingRepository = new EfListingRepository(verificationContext);
        var reservationRepository = new EfReservationRepository(verificationContext);
        var availabilityHandler = new CheckListingAvailabilityHandler(listingRepository, reservationRepository);
        var createReservationHandler = new CreateReservationHandler(
            listingRepository,
            reservationRepository,
            new EfApplicationUnitOfWork(verificationContext),
            new EfAuditLog(verificationContext, new FixedClock(DateTimeOffset.UtcNow)),
            new FixedClock(DateTimeOffset.UtcNow));

        var publicListing = await listingRepository.GetPublishedByIdAsync(listingId, CancellationToken.None);
        var publicSearch = await listingRepository.SearchPublishedAsync("Lifecycle archive", page: 1, pageSize: 20, CancellationToken.None);
        var availability = await availabilityHandler.HandleAsync(new CheckListingAvailabilityQuery(
            listingId,
            new DateOnly(2027, 8, 13),
            new DateOnly(2027, 8, 15)), CancellationToken.None);
        var reservationResult = await createReservationHandler.HandleAsync(new CreateReservationCommand(
            Guid.NewGuid(),
            listingId,
            nextGuestUserId,
            new DateOnly(2027, 8, 13),
            new DateOnly(2027, 8, 15)), CancellationToken.None);
        var guestReservations = await reservationRepository.SearchGuestReservationsAsync(guestUserId, page: 1, pageSize: 20, CancellationToken.None);
        var auditEventExists = await verificationContext.AuditEvents.AnyAsync(auditEvent =>
            auditEvent.EventType == AuditEventTypes.ListingArchived &&
            auditEvent.SubjectId == listingId);

        Assert.Null(publicListing);
        Assert.Empty(publicSearch.Items);
        Assert.False(await listingRepository.IsPublishedAsync(listingId, CancellationToken.None));
        Assert.False(availability.Succeeded);
        Assert.Equal(ListingAvailabilityError.ListingNotFound, availability.Error);
        Assert.False(reservationResult.Succeeded);
        Assert.Equal(CreateReservationError.ListingUnavailable, reservationResult.Error);
        Assert.Single(guestReservations.Items);
        Assert.Equal(existingReservationId, guestReservations.Items[0].Id);
        Assert.True(auditEventExists);
    }

    [DockerAvailableFact]
    public async Task UnpublishListingHandler_RemovesListingFromPublicVisibilityAndCanBeRepublished()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var ownerUserId = await CreateUserAsync(fixture, "owner-unpublish");
        var listingId = await CreateListingAsync(fixture, ownerUserId, "Lifecycle unpublish listing");

        await using (var dbContext = fixture.CreateDbContext())
        {
            var handler = new UnpublishListingHandler(
                new EfListingRepository(dbContext),
                new EfApplicationUnitOfWork(dbContext),
                new EfAuditLog(dbContext, new FixedClock(DateTimeOffset.UtcNow)));

            var result = await handler.HandleAsync(new UpdateListingStatusCommand(listingId, ownerUserId), CancellationToken.None);

            Assert.True(result.Succeeded);
            Assert.Equal("Draft", result.Status);
        }

        await using (var verificationContext = fixture.CreateDbContext())
        {
            var repository = new EfListingRepository(verificationContext);
            var publicListing = await repository.GetPublishedByIdAsync(listingId, CancellationToken.None);
            var listing = await repository.GetByIdAsync(listingId, CancellationToken.None);

            Assert.Null(publicListing);
            Assert.NotNull(listing);
            Assert.Equal(ListingStatus.Draft, listing.Status);
        }

        await using (var republishContext = fixture.CreateDbContext())
        {
            var handler = new PublishListingHandler(
                new EfListingRepository(republishContext),
                new EfApplicationUnitOfWork(republishContext),
                new EfAuditLog(republishContext, new FixedClock(DateTimeOffset.UtcNow)));

            var result = await handler.HandleAsync(new PublishListingCommand(listingId, ownerUserId), CancellationToken.None);

            Assert.True(result.Succeeded);
        }
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

    private static async Task<Guid> CreateListingAsync(PostgreSqlFixture fixture, Guid ownerUserId, string title)
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, title, DateTimeOffset.UtcNow);
        listing.Publish();

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        return listing.Id;
    }

    private static async Task<Guid> CreateReservationAsync(
        PostgreSqlFixture fixture,
        Guid listingId,
        Guid guestUserId,
        DateOnly startDate,
        DateOnly endDate)
    {
        var reservation = Reservation.CreatePending(
            Guid.NewGuid(),
            listingId,
            guestUserId,
            DateRange.Create(startDate, endDate),
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
