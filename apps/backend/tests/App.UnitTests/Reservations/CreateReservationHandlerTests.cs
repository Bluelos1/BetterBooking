using App.Application.Common;
using App.Application.Audit;
using App.Application.Listings;
using App.Application.Reservations;
using App.Domain.Availability;
using App.Domain.Listings;
using App.Domain.Reservations;

namespace App.UnitTests.Reservations;

public sealed class CreateReservationHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenListingIsAvailable_AddsReservationInsideTransaction()
    {
        var listingRepository = new FakeListingRepository(isPublished: true);
        var repository = new FakeReservationRepository(hasOverlap: false);
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2027, 1, 1, 10, 0, 0, TimeSpan.Zero));
        var auditLog = new FakeAuditLog();
        var handler = new CreateReservationHandler(listingRepository, repository, unitOfWork, auditLog, clock);
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2027, 5, 10),
            new DateOnly(2027, 5, 12));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(command.ReservationId, result.ReservationId);
        Assert.Equal(1, unitOfWork.TransactionCount);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Single(repository.AddedReservations);
        Assert.Equal(clock.UtcNow, repository.AddedReservations[0].CreatedAt);
        Assert.Contains(auditLog.Entries, entry => entry.EventType == AuditEventTypes.ReservationCreated);
    }

    [Fact]
    public async Task HandleAsync_WhenListingHasActiveOverlap_ReturnsUnavailableAndDoesNotAddReservation()
    {
        var listingRepository = new FakeListingRepository(isPublished: true);
        var repository = new FakeReservationRepository(hasOverlap: true);
        var unitOfWork = new FakeUnitOfWork();
        var auditLog = new FakeAuditLog();
        var handler = new CreateReservationHandler(listingRepository, repository, unitOfWork, auditLog, new FixedClock(DateTimeOffset.UtcNow));
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2027, 5, 10),
            new DateOnly(2027, 5, 12));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(CreateReservationError.ListingUnavailable, result.Error);
        Assert.Equal(1, unitOfWork.TransactionCount);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Empty(repository.AddedReservations);
        Assert.Contains(auditLog.Entries, entry => entry.EventType == AuditEventTypes.ReservationRejected);
    }

    [Fact]
    public async Task HandleAsync_WhenListingIsNotPublished_ReturnsUnavailableAndDoesNotCheckOverlaps()
    {
        var listingRepository = new FakeListingRepository(isPublished: false);
        var repository = new FakeReservationRepository(hasOverlap: false);
        var unitOfWork = new FakeUnitOfWork();
        var auditLog = new FakeAuditLog();
        var handler = new CreateReservationHandler(listingRepository, repository, unitOfWork, auditLog, new FixedClock(DateTimeOffset.UtcNow));
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2027, 5, 10),
            new DateOnly(2027, 5, 12));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(CreateReservationError.ListingUnavailable, result.Error);
        Assert.Equal(0, repository.HasActiveOverlapCallCount);
        Assert.Empty(repository.AddedReservations);
        Assert.Contains(auditLog.Entries, entry => entry.EventType == AuditEventTypes.ReservationRejected);
    }

    [Fact]
    public async Task HandleAsync_WhenDatabaseRejectsReservationOverlap_ReturnsUnavailable()
    {
        var listingRepository = new FakeListingRepository(isPublished: true);
        var repository = new FakeReservationRepository(hasOverlap: false);
        var unitOfWork = new FakeUnitOfWork(throwReservationConflict: true);
        var handler = new CreateReservationHandler(listingRepository, repository, unitOfWork, new FakeAuditLog(), new FixedClock(DateTimeOffset.UtcNow));
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2027, 5, 10),
            new DateOnly(2027, 5, 12));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(CreateReservationError.ListingUnavailable, result.Error);
    }

    private sealed class FakeListingRepository : IListingRepository
    {
        private readonly bool _isPublished;

        public FakeListingRepository(bool isPublished)
        {
            _isPublished = isPublished;
        }

        public Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ListingReadModel?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> IsPublishedAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_isPublished);
        }

        public Task<ListingSearchResult> SearchPublishedAsync(
            string? searchTerm,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<OwnerListingSearchResult> SearchOwnerListingsAsync(
            Guid ownerUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Listing listing, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeReservationRepository : IReservationRepository
    {
        private readonly bool _hasOverlap;

        public FakeReservationRepository(bool hasOverlap)
        {
            _hasOverlap = hasOverlap;
        }

        public List<Reservation> AddedReservations { get; } = [];

        public int HasActiveOverlapCallCount { get; private set; }

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> HasActiveOverlapAsync(Guid listingId, DateRange period, CancellationToken cancellationToken)
        {
            HasActiveOverlapCallCount++;

            return Task.FromResult(_hasOverlap);
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
            AddedReservations.Add(reservation);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IApplicationUnitOfWork
    {
        private readonly bool _throwReservationConflict;

        public FakeUnitOfWork(bool throwReservationConflict = false)
        {
            _throwReservationConflict = throwReservationConflict;
        }

        public int TransactionCount { get; private set; }

        public int SaveChangesCount { get; private set; }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            TransactionCount++;

            if (_throwReservationConflict)
            {
                throw new ReservationConflictException("Overlap rejected by database.", new InvalidOperationException());
            }

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
