namespace App.Api.Reservations;

public sealed record ConfirmReservationPaymentResponse(
    Guid ReservationId,
    string Status,
    string PaymentStatus);
