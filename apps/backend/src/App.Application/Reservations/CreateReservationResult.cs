namespace App.Application.Reservations;

public sealed record CreateReservationResult(
    bool Succeeded,
    Guid? ReservationId,
    CreateReservationError? Error,
    string? Detail)
{
    public static CreateReservationResult Created(Guid reservationId) => new(true, reservationId, null, null);

    public static CreateReservationResult ListingUnavailable() => new(
        false,
        null,
        CreateReservationError.ListingUnavailable,
        "The listing is not available for the selected dates.");

    public static CreateReservationResult ValidationFailed(string detail) => new(
        false,
        null,
        CreateReservationError.ValidationFailed,
        detail);
}
