namespace App.Application.Listings;

public sealed record ListingReadModel(
    Guid Id,
    string Title,
    string Description,
    string Location,
    decimal NightlyPriceAmount,
    int MaxGuests,
    int BedroomCount,
    int BathroomCount,
    string HeroImageUrl,
    string Amenities,
    DateTimeOffset CreatedAt);
