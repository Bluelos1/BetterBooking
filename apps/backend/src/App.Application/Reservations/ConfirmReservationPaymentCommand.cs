namespace App.Application.Reservations;

public sealed record ConfirmReservationPaymentCommand(Guid ReservationId, Guid GuestUserId);
