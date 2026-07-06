using App.Application.Reservations;
using App.Domain.Availability;
using App.Domain.Reservations;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence.Repositories;

public sealed class EfReservationRepository : IReservationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfReservationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Reservations.FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);
    }

    public Task<bool> HasActiveOverlapAsync(Guid listingId, DateRange period, CancellationToken cancellationToken)
    {
        return _dbContext.Reservations.AnyAsync(reservation =>
            reservation.ListingId == listingId &&
            (reservation.Status == ReservationStatus.Pending || reservation.Status == ReservationStatus.Confirmed) &&
            reservation.Period.StartDate < period.EndDate &&
            period.StartDate < reservation.Period.EndDate,
            cancellationToken);
    }

    public async Task<ReservationSearchResult> SearchGuestReservationsAsync(
        Guid guestUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.GuestUserId == guestUserId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Join(
                _dbContext.Listings.AsNoTracking(),
                reservation => reservation.ListingId,
                listing => listing.Id,
                (reservation, listing) => new { Reservation = reservation, Listing = listing })
            .OrderByDescending(row => row.Reservation.CreatedAt)
            .ThenBy(row => row.Reservation.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new ReservationReadModel(
                row.Reservation.Id,
                row.Reservation.ListingId,
                row.Listing.Title,
                row.Reservation.Period.StartDate,
                row.Reservation.Period.EndDate,
                row.Reservation.Status.ToString(),
                row.Reservation.PaymentStatus.ToString(),
                row.Reservation.CreatedAt,
                row.Reservation.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new ReservationSearchResult(items, page, pageSize, totalCount);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        await _dbContext.Reservations.AddAsync(reservation, cancellationToken);
    }
}
