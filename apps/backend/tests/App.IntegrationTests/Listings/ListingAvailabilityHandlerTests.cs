using App.Application.Listings;
using App.Domain.Availability;
using App.Domain.Listings;
using App.Domain.Reservations;
using App.Domain.Users;
using App.Infrastructure.Persistence.Repositories;
using App.IntegrationTests.TestSupport;

namespace App.IntegrationTests.Listings;

public sealed class ListingAvailabilityHandlerTests
{
    [DockerAvailableFact]
    public async Task CheckListingAvailabilityHandler_UsesPublishedListingAndActiveReservationOverlap()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var ownerUserId = await CreateUserAsync(fixture, "owner-availability");
        var guestUserId = await CreateUserAsync(fixture, "guest-availability");
        var publishedListingId = await CreateListingAsync(fixture, ownerUserId, publish: true);
        var draftListingId = await CreateListingAsync(fixture, ownerUserId, publish: false);
        await CreateReservationAsync(fixture, publishedListingId, guestUserId);

        await using var dbContext = fixture.CreateDbContext();
        var handler = new CheckListingAvailabilityHandler(
            new EfListingRepository(dbContext),
            new EfReservationRepository(dbContext));

        var overlappingResult = await handler.HandleAsync(new CheckListingAvailabilityQuery(
            publishedListingId,
            new DateOnly(2027, 6, 11),
            new DateOnly(2027, 6, 13)), CancellationToken.None);
        var adjacentResult = await handler.HandleAsync(new CheckListingAvailabilityQuery(
            publishedListingId,
            new DateOnly(2027, 6, 14),
            new DateOnly(2027, 6, 16)), CancellationToken.None);
        var draftResult = await handler.HandleAsync(new CheckListingAvailabilityQuery(
            draftListingId,
            new DateOnly(2027, 6, 11),
            new DateOnly(2027, 6, 13)), CancellationToken.None);

        Assert.True(overlappingResult.Succeeded);
        Assert.False(overlappingResult.Available);
        Assert.True(adjacentResult.Succeeded);
        Assert.True(adjacentResult.Available);
        Assert.False(draftResult.Succeeded);
        Assert.Equal(ListingAvailabilityError.ListingNotFound, draftResult.Error);
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

    private static async Task<Guid> CreateListingAsync(PostgreSqlFixture fixture, Guid ownerUserId, bool publish)
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "Availability test listing", DateTimeOffset.UtcNow);

        if (publish)
        {
            listing.Publish();
        }

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        return listing.Id;
    }

    private static async Task CreateReservationAsync(PostgreSqlFixture fixture, Guid listingId, Guid guestUserId)
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Reservations.Add(Reservation.CreatePending(
            Guid.NewGuid(),
            listingId,
            guestUserId,
            DateRange.Create(new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 14)),
            DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();
    }
}
