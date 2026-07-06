using App.Domain.Listings;
using App.Domain.Users;
using App.Infrastructure.Persistence.Repositories;
using App.IntegrationTests.TestSupport;

namespace App.IntegrationTests.Listings;

public sealed class ListingReadRepositoryTests
{
    [DockerAvailableFact]
    public async Task SearchPublishedAsync_ReturnsOnlyPublishedListingsMatchingQuery()
    {
        await using var fixture = await PostgreSqlFixture.StartAsync();
        await fixture.ResetAsync();

        var ownerUserId = await CreateUserAsync(fixture, "owner-listing-read");
        var cityListingId = await CreateListingAsync(
            fixture,
            ownerUserId,
            "City apartment",
            new DateTimeOffset(2027, 1, 2, 0, 0, 0, TimeSpan.Zero),
            publish: true);
        await CreateListingAsync(
            fixture,
            ownerUserId,
            "Lakeside cabin",
            new DateTimeOffset(2027, 1, 3, 0, 0, 0, TimeSpan.Zero),
            publish: true);
        var draftListingId = await CreateListingAsync(
            fixture,
            ownerUserId,
            "City draft",
            new DateTimeOffset(2027, 1, 4, 0, 0, 0, TimeSpan.Zero),
            publish: false);

        await using var dbContext = fixture.CreateDbContext();
        var repository = new EfListingRepository(dbContext);

        var searchResult = await repository.SearchPublishedAsync("CITY", page: 1, pageSize: 10, CancellationToken.None);
        var cityListing = await repository.GetPublishedByIdAsync(cityListingId, CancellationToken.None);
        var draftListing = await repository.GetPublishedByIdAsync(draftListingId, CancellationToken.None);

        Assert.Single(searchResult.Items);
        Assert.Equal(cityListingId, searchResult.Items[0].Id);
        Assert.Equal(1, searchResult.TotalCount);
        Assert.NotNull(cityListing);
        Assert.Null(draftListing);
        Assert.True(await repository.IsPublishedAsync(cityListingId, CancellationToken.None));
        Assert.False(await repository.IsPublishedAsync(draftListingId, CancellationToken.None));
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
        DateTimeOffset createdAt,
        bool publish)
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, title, createdAt);

        if (publish)
        {
            listing.Publish();
        }

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        return listing.Id;
    }
}
