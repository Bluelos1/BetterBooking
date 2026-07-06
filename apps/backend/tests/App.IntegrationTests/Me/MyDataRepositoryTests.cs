using App.Domain.Availability;
using App.Domain.Listings;
using App.Domain.Reservations;
using App.Domain.Users;
using App.Infrastructure.Persistence.Repositories;
using App.IntegrationTests.TestSupport;

namespace App.IntegrationTests.Me;

public sealed class MyDataRepositoryTests
{
    [DockerAvailableFact]
    public async Task MyDataQueries_ReturnOnlyCurrentUserData()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var firstOwnerId = await CreateUserAsync(fixture, "owner-my-data-1");
        var secondOwnerId = await CreateUserAsync(fixture, "owner-my-data-2");
        var firstGuestId = await CreateUserAsync(fixture, "guest-my-data-1");
        var secondGuestId = await CreateUserAsync(fixture, "guest-my-data-2");
        var firstOwnerListingId = await CreateListingAsync(fixture, firstOwnerId, "First owner listing", publish: true);
        await CreateListingAsync(fixture, firstOwnerId, "First owner draft", publish: false);
        var secondOwnerListingId = await CreateListingAsync(fixture, secondOwnerId, "Second owner listing", publish: true);
        var firstGuestReservationId = await CreateReservationAsync(
            fixture,
            firstOwnerListingId,
            firstGuestId,
            new DateOnly(2027, 6, 10),
            new DateOnly(2027, 6, 12));
        await CreateReservationAsync(
            fixture,
            secondOwnerListingId,
            secondGuestId,
            new DateOnly(2027, 6, 13),
            new DateOnly(2027, 6, 15));

        await using var dbContext = fixture.CreateDbContext();
        var listingRepository = new EfListingRepository(dbContext);
        var reservationRepository = new EfReservationRepository(dbContext);

        var listingResult = await listingRepository.SearchOwnerListingsAsync(firstOwnerId, page: 1, pageSize: 20, CancellationToken.None);
        var reservationResult = await reservationRepository.SearchGuestReservationsAsync(firstGuestId, page: 1, pageSize: 20, CancellationToken.None);

        Assert.Equal(2, listingResult.TotalCount);
        Assert.All(listingResult.Items, listing => Assert.Contains("First owner", listing.Title, StringComparison.Ordinal));
        Assert.Single(reservationResult.Items);
        Assert.Equal(firstGuestReservationId, reservationResult.Items[0].Id);
        Assert.Equal("First owner listing", reservationResult.Items[0].ListingTitle);
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

    private static async Task<Guid> CreateListingAsync(
        PostgreSqlFixture fixture,
        Guid ownerUserId,
        string title,
        bool publish)
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, title, DateTimeOffset.UtcNow);

        if (publish)
        {
            listing.Publish();
        }

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
}
