namespace App.Application.Listings;

public sealed record UpdateListingStatusCommand(
    Guid ListingId,
    Guid OwnerUserId);
