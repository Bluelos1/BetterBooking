namespace App.Application.Reservations;

public sealed class GetGuestReservationsHandler
{
    private const int MaxPageSize = 50;
    private readonly IReservationRepository _reservationRepository;

    public GetGuestReservationsHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public Task<ReservationSearchResult> HandleAsync(GetGuestReservationsQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        return _reservationRepository.SearchGuestReservationsAsync(query.GuestUserId, page, pageSize, cancellationToken);
    }
}
