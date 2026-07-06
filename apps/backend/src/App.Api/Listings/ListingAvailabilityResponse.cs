namespace App.Api.Listings;

public sealed record ListingAvailabilityResponse(
    Guid ListingId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool Available);
