namespace App.Application.Listings;

public sealed record OwnerListingReadModel(
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
    string Status,
    DateTimeOffset CreatedAt);
