namespace App.Application.Listings;

public sealed record CreateListingResult(
    bool Succeeded,
    Guid? ListingId,
    CreateListingError? Error,
    string? Detail)
{
    public static CreateListingResult Created(Guid listingId) => new(true, listingId, null, null);

    public static CreateListingResult ValidationFailed(string detail) => new(
        false,
        null,
        CreateListingError.ValidationFailed,
        detail);
}
