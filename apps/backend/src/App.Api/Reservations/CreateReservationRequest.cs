namespace App.Api.Reservations;

public sealed record CreateReservationRequest(
    Guid ListingId,
    DateOnly StartDate,
    DateOnly EndDate);
