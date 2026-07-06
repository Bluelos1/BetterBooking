namespace App.Application.Reservations;

public sealed record ConfirmReservationPaymentResult(
    bool Succeeded,
    Guid? ReservationId,
    string? Status,
    string? PaymentStatus,
    ConfirmReservationPaymentError? Error,
    string? Detail)
{
    public static ConfirmReservationPaymentResult Confirmed(Guid reservationId, string status, string paymentStatus) =>
        new(true, reservationId, status, paymentStatus, null, null);

    public static ConfirmReservationPaymentResult ValidationFailed(string detail) =>
        new(false, null, null, null, ConfirmReservationPaymentError.ValidationFailed, detail);

    public static ConfirmReservationPaymentResult NotFound() =>
        new(false, null, null, null, ConfirmReservationPaymentError.NotFound, "Reservation was not found.");

    public static ConfirmReservationPaymentResult Forbidden() =>
        new(false, null, null, null, ConfirmReservationPaymentError.Forbidden, "Only the reservation guest can pay for this reservation.");

    public static ConfirmReservationPaymentResult InvalidState(string detail) =>
        new(false, null, null, null, ConfirmReservationPaymentError.InvalidState, detail);
}
