using App.Domain.Availability;

namespace App.Domain.Reservations;

public sealed class Reservation
{
    private Reservation()
    {
    }

    private Reservation(
        Guid id,
        Guid listingId,
        Guid guestUserId,
        DateRange period,
        ReservationStatus status,
        PaymentStatus paymentStatus,
        DateTimeOffset createdAt)
    {
        Id = id;
        ListingId = listingId;
        GuestUserId = guestUserId;
        Period = period;
        Status = status;
        PaymentStatus = paymentStatus;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ListingId { get; private set; }

    public Guid GuestUserId { get; private set; }

    public DateRange Period { get; private set; }

    public ReservationStatus Status { get; private set; }

    public PaymentStatus PaymentStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Reservation CreatePending(Guid id, Guid listingId, Guid guestUserId, DateRange period, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Reservation id is required.", nameof(id));
        }

        if (listingId == Guid.Empty)
        {
            throw new ArgumentException("Listing id is required.", nameof(listingId));
        }

        if (guestUserId == Guid.Empty)
        {
            throw new ArgumentException("Guest user id is required.", nameof(guestUserId));
        }

        return new Reservation(id, listingId, guestUserId, period, ReservationStatus.Pending, PaymentStatus.Unpaid, createdAt);
    }

    public void ConfirmPayment(DateTimeOffset updatedAt)
    {
        EnsureStatus(ReservationStatus.Pending);

        if (PaymentStatus is not PaymentStatus.Unpaid)
        {
            throw new InvalidOperationException($"Reservation payment must be {PaymentStatus.Unpaid} but is {PaymentStatus}.");
        }

        PaymentStatus = PaymentStatus.Paid;
        Status = ReservationStatus.Confirmed;
        UpdatedAt = updatedAt;
    }

    public void Confirm(DateTimeOffset updatedAt)
    {
        EnsureStatus(ReservationStatus.Pending);
        Status = ReservationStatus.Confirmed;
        UpdatedAt = updatedAt;
    }

    public void Cancel(DateTimeOffset updatedAt)
    {
        if (Status is ReservationStatus.Expired)
        {
            throw new InvalidOperationException("Expired reservations cannot be cancelled.");
        }

        if (PaymentStatus is PaymentStatus.Paid)
        {
            PaymentStatus = PaymentStatus.Refunded;
        }

        Status = ReservationStatus.Cancelled;
        UpdatedAt = updatedAt;
    }

    public void Expire(DateTimeOffset updatedAt)
    {
        EnsureStatus(ReservationStatus.Pending);
        Status = ReservationStatus.Expired;
        UpdatedAt = updatedAt;
    }

    private void EnsureStatus(ReservationStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException($"Reservation must be {expectedStatus} but is {Status}.");
        }
    }
}
