namespace App.Application.Listings;

public sealed record PublishListingCommand(Guid ListingId, Guid OwnerUserId);
