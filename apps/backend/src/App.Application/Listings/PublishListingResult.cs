namespace App.Application.Listings;

public sealed record PublishListingResult(
    bool Succeeded,
    Guid? ListingId,
    PublishListingError? Error,
    string? Detail)
{
    public static PublishListingResult Published(Guid listingId) => new(true, listingId, null, null);

    public static PublishListingResult NotFound() => new(false, null, PublishListingError.NotFound, "Listing was not found.");

    public static PublishListingResult Forbidden() => new(false, null, PublishListingError.Forbidden, "Only the listing owner can perform this action.");

    public static PublishListingResult InvalidState(string detail) => new(false, null, PublishListingError.InvalidState, detail);
}
