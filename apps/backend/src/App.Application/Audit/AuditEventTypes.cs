namespace App.Application.Audit;

public static class AuditEventTypes
{
    public const string AuthorizationFailed = "authorization.failed";
    public const string ListingArchived = "listing.archived";
    public const string ListingCreated = "listing.created";
    public const string ListingPublished = "listing.published";
    public const string ListingUnpublished = "listing.unpublished";
    public const string PaymentConfirmed = "payment.confirmed";
    public const string ReservationCancelled = "reservation.cancelled";
    public const string ReservationCreated = "reservation.created";
    public const string ReservationRejected = "reservation.rejected";
    public const string UserMapped = "user.mapped";
}
