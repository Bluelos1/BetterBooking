namespace App.Api.Listings;

public sealed record CreateListingRequest(
    string Title,
    string Description,
    string Location,
    decimal NightlyPriceAmount,
    int MaxGuests,
    int BedroomCount,
    int BathroomCount,
    string? HeroImageUrl,
    string? Amenities);
