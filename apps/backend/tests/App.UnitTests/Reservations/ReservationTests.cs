using App.Domain.Availability;
using App.Domain.Reservations;

namespace App.UnitTests.Reservations;

public sealed class ReservationTests
{
    [Fact]
    public void CreatePending_WithValidInput_CreatesPendingReservation()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var period = DateRange.Create(new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 3));

        var reservation = Reservation.CreatePending(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), period, createdAt);

        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Equal(PaymentStatus.Unpaid, reservation.PaymentStatus);
        Assert.Equal(createdAt, reservation.CreatedAt);
        Assert.Equal(createdAt, reservation.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenPending_ConfirmsReservation()
    {
        var reservation = CreatePendingReservation();
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        reservation.Confirm(updatedAt);

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(updatedAt, reservation.UpdatedAt);
    }

    [Fact]
    public void ConfirmPayment_WhenPendingUnpaid_MarksPaidAndConfirmsReservation()
    {
        var reservation = CreatePendingReservation();
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        reservation.ConfirmPayment(updatedAt);

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(PaymentStatus.Paid, reservation.PaymentStatus);
        Assert.Equal(updatedAt, reservation.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenCancelled_Throws()
    {
        var reservation = CreatePendingReservation();
        reservation.Cancel(DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => reservation.Confirm(DateTimeOffset.UtcNow.AddMinutes(2)));
    }

    [Fact]
    public void Cancel_WhenExpired_Throws()
    {
        var reservation = CreatePendingReservation();
        reservation.Expire(DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => reservation.Cancel(DateTimeOffset.UtcNow.AddMinutes(2)));
    }

    private static Reservation CreatePendingReservation()
    {
        var period = DateRange.Create(new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 3));

        return Reservation.CreatePending(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), period, DateTimeOffset.UtcNow);
    }
}
