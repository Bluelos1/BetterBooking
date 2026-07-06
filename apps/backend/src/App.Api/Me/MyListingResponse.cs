namespace App.Api.Me;

public sealed record MyListingResponse(
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
