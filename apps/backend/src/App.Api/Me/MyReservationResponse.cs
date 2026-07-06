namespace App.Api.Me;

public sealed record MyReservationResponse(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string PaymentStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
