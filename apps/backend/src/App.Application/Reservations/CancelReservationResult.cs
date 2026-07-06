namespace App.Application.Reservations;

public sealed record CancelReservationResult(
    bool Succeeded,
    Guid? ReservationId,
    string? PaymentStatus,
    CancelReservationError? Error,
    string? Detail)
{
    public static CancelReservationResult Cancelled(Guid reservationId, string paymentStatus) => new(true, reservationId, paymentStatus, null, null);

    public static CancelReservationResult ValidationFailed(string detail) => new(
        false,
        null,
        null,
        CancelReservationError.ValidationFailed,
        detail);

    public static CancelReservationResult NotFound() => new(false, null, null, CancelReservationError.NotFound, "Reservation was not found.");

    public static CancelReservationResult Forbidden() => new(
        false,
        null,
        null,
        CancelReservationError.Forbidden,
        "Only the reservation guest can perform this action.");

    public static CancelReservationResult InvalidState(string detail) => new(false, null, null, CancelReservationError.InvalidState, detail);
}
