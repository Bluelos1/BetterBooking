namespace App.Application.Listings;

public sealed record CheckListingAvailabilityQuery(
    Guid ListingId,
    DateOnly StartDate,
    DateOnly EndDate);
