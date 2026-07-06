using App.Domain.Availability;
using App.Domain.Reservations;

namespace App.Application.Reservations;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasActiveOverlapAsync(Guid listingId, DateRange period, CancellationToken cancellationToken);

    Task<ReservationSearchResult> SearchGuestReservationsAsync(Guid guestUserId, int page, int pageSize, CancellationToken cancellationToken);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
}
