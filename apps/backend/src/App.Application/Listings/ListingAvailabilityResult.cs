namespace App.Application.Listings;

public sealed record ListingAvailabilityResult(
    bool Succeeded,
    Guid ListingId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool? Available,
    ListingAvailabilityError? Error,
    string? Detail)
{
    public static ListingAvailabilityResult Checked(Guid listingId, DateOnly startDate, DateOnly endDate, bool available) => new(
        true,
        listingId,
        startDate,
        endDate,
        available,
        null,
        null);

    public static ListingAvailabilityResult ListingNotFound(Guid listingId, DateOnly startDate, DateOnly endDate) => new(
        false,
        listingId,
        startDate,
        endDate,
        null,
        ListingAvailabilityError.ListingNotFound,
        "Listing was not found.");

    public static ListingAvailabilityResult ValidationFailed(Guid listingId, DateOnly startDate, DateOnly endDate, string detail) => new(
        false,
        listingId,
        startDate,
        endDate,
        null,
        ListingAvailabilityError.ValidationFailed,
        detail);
}
