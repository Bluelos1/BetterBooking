namespace App.Domain.Availability;

public sealed class AvailabilityWindow
{
    private AvailabilityWindow(Guid listingId, DateRange period, bool isAvailable)
    {
        ListingId = listingId;
        Period = period;
        IsAvailable = isAvailable;
    }

    public Guid ListingId { get; }

    public DateRange Period { get; }

    public bool IsAvailable { get; }

    public static AvailabilityWindow Create(Guid listingId, DateRange period, bool isAvailable)
    {
        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("Listing id is required.", nameof(listingId));
        }

        return new AvailabilityWindow(listingId, period, isAvailable);
    }
}
