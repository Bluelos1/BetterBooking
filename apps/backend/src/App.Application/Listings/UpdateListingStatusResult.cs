namespace App.Application.Listings;

public sealed record UpdateListingStatusResult(
    bool Succeeded,
    Guid? ListingId,
    string? Status,
    UpdateListingStatusError? Error,
    string? Detail)
{
    public static UpdateListingStatusResult Updated(Guid listingId, string status) => new(true, listingId, status, null, null);

    public static UpdateListingStatusResult NotFound() => new(false, null, null, UpdateListingStatusError.NotFound, "Listing was not found.");

    public static UpdateListingStatusResult Forbidden() => new(
        false,
        null,
        null,
        UpdateListingStatusError.Forbidden,
        "Only the listing owner can perform this action.");

    public static UpdateListingStatusResult InvalidState(string detail) => new(false, null, null, UpdateListingStatusError.InvalidState, detail);
}
