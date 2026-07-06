namespace App.Api.Reservations;

public sealed record CreateReservationResponse(Guid ReservationId, string Status, string PaymentStatus);
