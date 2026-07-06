namespace App.Api.Reservations;

public sealed record CancelReservationResponse(
    Guid ReservationId,
    string Status,
    string PaymentStatus);
