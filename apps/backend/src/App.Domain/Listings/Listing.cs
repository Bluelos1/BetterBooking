namespace App.Domain.Listings;

public sealed class Listing
{
    private Listing()
    {
        Title = string.Empty;
        Description = string.Empty;
        Location = string.Empty;
        HeroImageUrl = string.Empty;
        Amenities = string.Empty;
    }

    private Listing(
        Guid id,
        Guid ownerUserId,
        string title,
        string description,
        string location,
        decimal nightlyPriceAmount,
        int maxGuests,
        int bedroomCount,
        int bathroomCount,
        string heroImageUrl,
        string amenities,
        ListingStatus status,
        DateTimeOffset createdAt)
    {
        Id = id;
        OwnerUserId = ownerUserId;
        Title = title;
        Description = description;
        Location = location;
        NightlyPriceAmount = nightlyPriceAmount;
        MaxGuests = maxGuests;
        BedroomCount = bedroomCount;
        BathroomCount = bathroomCount;
        HeroImageUrl = heroImageUrl;
        Amenities = amenities;
        Status = status;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public string Location { get; private set; }

    public decimal NightlyPriceAmount { get; private set; }

    public int MaxGuests { get; private set; }

    public int BedroomCount { get; private set; }

    public int BathroomCount { get; private set; }

    public string HeroImageUrl { get; private set; }

    public string Amenities { get; private set; }

    public ListingStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Listing CreateDraft(Guid id, Guid ownerUserId, string title, DateTimeOffset createdAt)
    {
        return CreateDraft(
            id,
            ownerUserId,
            title,
            "A comfortable stay with the essentials configured for local development.",
            "Local Test District",
            120m,
            2,
            1,
            1,
            null,
            "Wi-Fi, Kitchen",
            createdAt);
    }

    public static Listing CreateDraft(
        Guid id,
        Guid ownerUserId,
        string title,
        string description,
        string location,
        decimal nightlyPriceAmount,
        int maxGuests,
        int bedroomCount,
        int bathroomCount,
        string? heroImageUrl,
        string? amenities,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Listing id is required.", nameof(id));
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("Owner user id is required.", nameof(ownerUserId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Listing title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Listing description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Listing location is required.", nameof(location));
        }

        if (nightlyPriceAmount <= 0)
        {
            throw new ArgumentException("Nightly price must be greater than zero.", nameof(nightlyPriceAmount));
        }

        if (maxGuests < 1)
        {
            throw new ArgumentException("Maximum guests must be at least one.", nameof(maxGuests));
        }

        if (bedroomCount < 0)
        {
            throw new ArgumentException("Bedroom count cannot be negative.", nameof(bedroomCount));
        }

        if (bathroomCount < 1)
        {
            throw new ArgumentException("Bathroom count must be at least one.", nameof(bathroomCount));
        }

        return new Listing(
            id,
            ownerUserId,
            title.Trim(),
            description.Trim(),
            location.Trim(),
            nightlyPriceAmount,
            maxGuests,
            bedroomCount,
            bathroomCount,
            heroImageUrl?.Trim() ?? string.Empty,
            amenities?.Trim() ?? string.Empty,
            ListingStatus.Draft,
            createdAt);
    }

    public bool IsOwnedBy(Guid userId) => OwnerUserId == userId;

    public void Publish()
    {
        if (Status is ListingStatus.Archived)
        {
            throw new InvalidOperationException("Archived listings cannot be published.");
        }

        Status = ListingStatus.Published;
    }

    public void Unpublish()
    {
        if (Status is ListingStatus.Archived)
        {
            throw new InvalidOperationException("Archived listings cannot be unpublished.");
        }

        if (Status is not ListingStatus.Published)
        {
            throw new InvalidOperationException("Only published listings can be unpublished.");
        }

        Status = ListingStatus.Draft;
    }

    public void Archive() => Status = ListingStatus.Archived;
}
