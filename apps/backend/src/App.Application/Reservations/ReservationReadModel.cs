namespace App.Application.Reservations;

public sealed record ReservationReadModel(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string PaymentStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
