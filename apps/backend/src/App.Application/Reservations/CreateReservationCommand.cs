namespace App.Application.Reservations;

public sealed record CreateReservationCommand(
    Guid ReservationId,
    Guid ListingId,
    Guid GuestUserId,
    DateOnly StartDate,
    DateOnly EndDate);
