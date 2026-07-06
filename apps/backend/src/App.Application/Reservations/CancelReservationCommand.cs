namespace App.Application.Reservations;

public sealed record CancelReservationCommand(
    Guid ReservationId,
    Guid GuestUserId);
