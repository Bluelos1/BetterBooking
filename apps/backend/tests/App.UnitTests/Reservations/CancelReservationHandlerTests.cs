using App.Application.Audit;
using App.Application.Common;
using App.Application.Reservations;
using App.Domain.Availability;
using App.Domain.Reservations;

namespace App.UnitTests.Reservations;

public sealed class CancelReservationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenGuestOwnsReservation_CancelsAndAuditLogs()
    {
        var guestUserId = Guid.NewGuid();
        var reservation = Reservation.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            guestUserId,
            DateRange.Create(new DateOnly(2027, 5, 10), new DateOnly(2027, 5, 12)),
            DateTimeOffset.UtcNow);
        var repository = new FakeReservationRepository(reservation);
        var unitOfWork = new FakeUnitOfWork();
        var auditLog = new FakeAuditLog();
        var clock = new FixedClock(new DateTimeOffset(2027, 5, 1, 0, 0, 0, TimeSpan.Zero));
        var handler = new CancelReservationHandler(repository, unitOfWork, auditLog, clock);

        var result = await handler.HandleAsync(new CancelReservationCommand(reservation.Id, guestUserId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(clock.UtcNow, reservation.UpdatedAt);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Contains(auditLog.Entries, entry => entry.EventType == AuditEventTypes.ReservationCancelled);
    }

    [Fact]
    public async Task HandleAsync_WhenCallerIsNotGuest_ReturnsForbidden()
    {
        var reservation = Reservation.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateRange.Create(new DateOnly(2027, 5, 10), new DateOnly(2027, 5, 12)),
            DateTimeOffset.UtcNow);
        var repository = new FakeReservationRepository(reservation);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelReservationHandler(repository, unitOfWork, new FakeAuditLog(), new FixedClock(DateTimeOffset.UtcNow));

        var result = await handler.HandleAsync(new CancelReservationCommand(reservation.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(CancelReservationError.Forbidden, result.Error);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private sealed class FakeReservationRepository : IReservationRepository
    {
        private readonly Reservation? _reservation;

        public FakeReservationRepository(Reservation? reservation)
        {
            _reservation = reservation;
        }

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_reservation?.Id == id ? _reservation : null);
        }

        public Task<bool> HasActiveOverlapAsync(Guid listingId, DateRange period, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ReservationSearchResult> SearchGuestReservationsAsync(
            Guid guestUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeUnitOfWork : IApplicationUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            return await operation(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLog : IAuditLog
    {
        public List<AuditLogEntry> Entries { get; } = [];

        public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);

            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : ISystemClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
