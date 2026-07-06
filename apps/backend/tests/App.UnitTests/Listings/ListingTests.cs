using App.Domain.Listings;

namespace App.UnitTests.Listings;

public sealed class ListingTests
{
    [Fact]
    public void CreateDraft_TrimsTitleAndCreatesDraftListing()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var ownerUserId = Guid.NewGuid();

        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "  City apartment  ", createdAt);

        Assert.Equal("City apartment", listing.Title);
        Assert.Equal(ownerUserId, listing.OwnerUserId);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(createdAt, listing.CreatedAt);
    }

    [Fact]
    public void Publish_WhenListingIsArchived_Throws()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City apartment", DateTimeOffset.UtcNow);
        listing.Archive();

        Assert.Throws<InvalidOperationException>(listing.Publish);
    }

    [Fact]
    public void Unpublish_WhenListingIsPublished_MovesListingToDraft()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City apartment", DateTimeOffset.UtcNow);
        listing.Publish();

        listing.Unpublish();

        Assert.Equal(ListingStatus.Draft, listing.Status);
    }

    [Fact]
    public void Unpublish_WhenListingIsDraft_Throws()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City apartment", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(listing.Unpublish);
    }
}
