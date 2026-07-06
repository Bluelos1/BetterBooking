namespace App.Application.Listings;

public sealed record CreateListingCommand(
    Guid ListingId,
    Guid OwnerUserId,
    string Title,
    string Description,
    string Location,
    decimal NightlyPriceAmount,
    int MaxGuests,
    int BedroomCount,
    int BathroomCount,
    string? HeroImageUrl,
    string? Amenities);
