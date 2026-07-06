using App.Application.Reservations;
using App.Domain.Availability;

namespace App.Application.Listings;

public sealed class CheckListingAvailabilityHandler
{
    private readonly IListingRepository _listingRepository;
    private readonly IReservationRepository _reservationRepository;

    public CheckListingAvailabilityHandler(
        IListingRepository listingRepository,
        IReservationRepository reservationRepository)
    {
        _listingRepository = listingRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<ListingAvailabilityResult> HandleAsync(
        CheckListingAvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ListingId == Guid.Empty)
        {
            return ListingAvailabilityResult.ValidationFailed(
                query.ListingId,
                query.StartDate,
                query.EndDate,
                "Listing id is required.");
        }

        DateRange period;

        try
        {
            period = DateRange.Create(query.StartDate, query.EndDate);
        }
        catch (ArgumentException exception)
        {
            return ListingAvailabilityResult.ValidationFailed(
                query.ListingId,
                query.StartDate,
                query.EndDate,
                exception.Message);
        }

        var isPublished = await _listingRepository.IsPublishedAsync(query.ListingId, cancellationToken);

        if (!isPublished)
        {
            return ListingAvailabilityResult.ListingNotFound(query.ListingId, query.StartDate, query.EndDate);
        }

        var hasOverlap = await _reservationRepository.HasActiveOverlapAsync(query.ListingId, period, cancellationToken);

        return ListingAvailabilityResult.Checked(query.ListingId, query.StartDate, query.EndDate, !hasOverlap);
    }
}
