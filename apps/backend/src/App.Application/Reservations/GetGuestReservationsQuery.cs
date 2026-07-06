namespace App.Application.Reservations;

public sealed record GetGuestReservationsQuery(
    Guid GuestUserId,
    int Page,
    int PageSize);
