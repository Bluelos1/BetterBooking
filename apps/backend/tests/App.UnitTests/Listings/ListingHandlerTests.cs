using App.Application.Audit;
using App.Application.Common;
using App.Application.Listings;
using App.Application.Reservations;
using App.Domain.Availability;
using App.Domain.Listings;
using App.Domain.Reservations;

namespace App.UnitTests.Listings;

public sealed class ListingHandlerTests
{
    [Fact]
    public async Task CreateListingHandler_WithValidCommand_CreatesDraftListingAndAuditEvent()
    {
        var repository = new FakeListingRepository();
        var unitOfWork = new FakeUnitOfWork();
        var auditLog = new FakeAuditLog();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var handler = new CreateListingHandler(repository, unitOfWork, auditLog, clock);
        var ownerUserId = Guid.NewGuid();

        var result = await handler.HandleAsync(new CreateListingCommand(
            Guid.NewGuid(),
            ownerUserId,
            "City apartment",
            "Bright apartment close to transit.",
            "Krakow, Old Town",
            180m,
            4,
            2,
            1,
            null,
            "Wi-Fi, Kitchen"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(repository.Listings);
        Assert.Equal(ownerUserId, repository.Listings[0].OwnerUserId);
        Assert.Equal(ListingStatus.Draft, repository.Listings[0].Status);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Contains(auditLog.Entries, entry => entry.EventType == AuditEventTypes.ListingCreated);
    }

    [Fact]
    public async Task PublishListingHandler_WhenCallerIsNotOwner_ReturnsForbidden()
    {
        var ownerUserId = Guid.NewGuid();
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "City apartment", DateTimeOffset.UtcNow);
        var repository = new FakeListingRepository(listing);
        var handler = new PublishListingHandler(repository, new FakeUnitOfWork(), new FakeAuditLog());

        var result = await handler.HandleAsync(new PublishListingCommand(listing.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(PublishListingError.Forbidden, result.Error);
        Assert.Equal(ListingStatus.Draft, listing.Status);
    }

    [Fact]
    public async Task PublishListingHandler_WhenCallerIsOwner_PublishesListingAndAuditEvent()
    {
        var ownerUserId = Guid.NewGuid();
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "City apartment", DateTimeOffset.UtcNow);
        var repository = new FakeListingRepository(listing);
        var unitOfWork = new FakeUnitOfWork();
        var auditLog = new FakeAuditLog();
        var handler = new PublishListingHandler(repository, unitOfWork, auditLog);

        var result = await handler.HandleAsync(new PublishListingCommand(listing.Id, ownerUserId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Contains(auditLog.Entries, entry => entry.EventType == AuditEventTypes.ListingPublished);
    }

    [Fact]
    public async Task UnpublishListingHandler_WhenCallerIsOwner_UnpublishesListingAndAuditEvent()
    {
        var ownerUserId = Guid.NewGuid();
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "City apartment", DateTimeOffset.UtcNow);
        listing.Publish();
        var repository = new FakeListingRepository(listing);
        var unitOfWork = new FakeUnitOfWork();
        var auditLog = new FakeAuditLog();
        var handler = new UnpublishListingHandler(repository, unitOfWork, auditLog);

        var result = await handler.HandleAsync(new UpdateListingStatusCommand(listing.Id, ownerUserId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Draft", result.Status);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Contains(auditLog.Entries, entry => entry.EventType == AuditEventTypes.ListingUnpublished);
    }

    [Fact]
    public async Task UnpublishListingHandler_WhenListingIsDraft_ReturnsInvalidState()
    {
        var ownerUserId = Guid.NewGuid();
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "City apartment", DateTimeOffset.UtcNow);
        var repository = new FakeListingRepository(listing);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UnpublishListingHandler(repository, unitOfWork, new FakeAuditLog());

        var result = await handler.HandleAsync(new UpdateListingStatusCommand(listing.Id, ownerUserId), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(UpdateListingStatusError.InvalidState, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ArchiveListingHandler_WhenCallerIsOwner_ArchivesListingAndAuditEvent()
    {
        var ownerUserId = Guid.NewGuid();
        var listing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "City apartment", DateTimeOffset.UtcNow);
        listing.Publish();
        var repository = new FakeListingRepository(listing);
        var unitOfWork = new FakeUnitOfWork();
        var auditLog = new FakeAuditLog();
        var handler = new ArchiveListingHandler(repository, unitOfWork, auditLog);

        var result = await handler.HandleAsync(new UpdateListingStatusCommand(listing.Id, ownerUserId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Archived", result.Status);
        Assert.Equal(ListingStatus.Archived, listing.Status);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Contains(auditLog.Entries, entry => entry.EventType == AuditEventTypes.ListingArchived);
    }

    [Fact]
    public async Task ArchiveListingHandler_WhenCallerIsNotOwner_ReturnsForbidden()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City apartment", DateTimeOffset.UtcNow);
        var repository = new FakeListingRepository(listing);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ArchiveListingHandler(repository, unitOfWork, new FakeAuditLog());

        var result = await handler.HandleAsync(new UpdateListingStatusCommand(listing.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(UpdateListingStatusError.Forbidden, result.Error);
        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task SearchListingsHandler_ReturnsPublishedListingsOnly()
    {
        var publishedListing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City apartment", DateTimeOffset.UtcNow);
        publishedListing.Publish();
        var draftListing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City draft", DateTimeOffset.UtcNow.AddMinutes(1));
        var repository = new FakeListingRepository(publishedListing, draftListing);
        var handler = new SearchListingsHandler(repository);

        var result = await handler.HandleAsync(new SearchListingsQuery("city", 1, 20), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(publishedListing.Id, result.Items[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetOwnerListingsHandler_ReturnsOnlyOwnerListings()
    {
        var ownerUserId = Guid.NewGuid();
        var ownerListing = Listing.CreateDraft(Guid.NewGuid(), ownerUserId, "Owner draft", DateTimeOffset.UtcNow);
        var otherListing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "Other draft", DateTimeOffset.UtcNow.AddMinutes(1));
        var repository = new FakeListingRepository(ownerListing, otherListing);
        var handler = new GetOwnerListingsHandler(repository);

        var result = await handler.HandleAsync(new GetOwnerListingsQuery(ownerUserId, 1, 20), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(ownerListing.Id, result.Items[0].Id);
        Assert.Equal("Draft", result.Items[0].Status);
    }

    [Fact]
    public async Task GetListingHandler_WhenListingIsDraft_ReturnsNull()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City apartment", DateTimeOffset.UtcNow);
        var repository = new FakeListingRepository(listing);
        var handler = new GetListingHandler(repository);

        var result = await handler.HandleAsync(listing.Id, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckListingAvailabilityHandler_WhenListingHasNoOverlap_ReturnsAvailable()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City apartment", DateTimeOffset.UtcNow);
        listing.Publish();
        var handler = new CheckListingAvailabilityHandler(
            new FakeListingRepository(listing),
            new FakeReservationRepository(hasOverlap: false));

        var result = await handler.HandleAsync(new CheckListingAvailabilityQuery(
            listing.Id,
            new DateOnly(2027, 5, 10),
            new DateOnly(2027, 5, 12)), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Available);
    }

    [Fact]
    public async Task CheckListingAvailabilityHandler_WhenListingIsNotPublished_ReturnsNotFound()
    {
        var listing = Listing.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), "City apartment", DateTimeOffset.UtcNow);
        var reservationRepository = new FakeReservationRepository(hasOverlap: false);
        var handler = new CheckListingAvailabilityHandler(
            new FakeListingRepository(listing),
            reservationRepository);

        var result = await handler.HandleAsync(new CheckListingAvailabilityQuery(
            listing.Id,
            new DateOnly(2027, 5, 10),
            new DateOnly(2027, 5, 12)), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ListingAvailabilityError.ListingNotFound, result.Error);
        Assert.Equal(0, reservationRepository.HasActiveOverlapCallCount);
    }

    private sealed class FakeListingRepository : IListingRepository
    {
        public FakeListingRepository(params Listing[] listings)
        {
            Listings.AddRange(listings);
        }

        public List<Listing> Listings { get; } = [];

        public Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Listings.FirstOrDefault(listing => listing.Id == id));
        }

        public Task<ListingReadModel?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var listing = Listings.FirstOrDefault(listing =>
                listing.Id == id && listing.Status == ListingStatus.Published);

            return Task.FromResult(listing is null ? null : ToReadModel(listing));
        }

        public Task<bool> IsPublishedAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Listings.Any(listing =>
                listing.Id == id && listing.Status == ListingStatus.Published));
        }

        public Task<ListingSearchResult> SearchPublishedAsync(
            string? searchTerm,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = Listings
                .Where(listing => listing.Status == ListingStatus.Published)
                .Where(listing => string.IsNullOrWhiteSpace(searchTerm) ||
                    listing.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(listing => listing.CreatedAt)
                .ThenBy(listing => listing.Id)
                .ToArray();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToReadModel)
                .ToArray();

            return Task.FromResult(new ListingSearchResult(items, page, pageSize, query.Length));
        }

        public Task<OwnerListingSearchResult> SearchOwnerListingsAsync(
            Guid ownerUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = Listings
                .Where(listing => listing.OwnerUserId == ownerUserId)
                .OrderByDescending(listing => listing.CreatedAt)
                .ThenBy(listing => listing.Id)
                .ToArray();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToOwnerReadModel)
                .ToArray();

            return Task.FromResult(new OwnerListingSearchResult(items, page, pageSize, query.Length));
        }

        public Task AddAsync(Listing listing, CancellationToken cancellationToken)
        {
            Listings.Add(listing);

            return Task.CompletedTask;
        }

        private static ListingReadModel ToReadModel(Listing listing)
        {
            return new ListingReadModel(
                listing.Id,
                listing.Title,
                listing.Description,
                listing.Location,
                listing.NightlyPriceAmount,
                listing.MaxGuests,
                listing.BedroomCount,
                listing.BathroomCount,
                listing.HeroImageUrl,
                listing.Amenities,
                listing.CreatedAt);
        }

        private static OwnerListingReadModel ToOwnerReadModel(Listing listing)
        {
            return new OwnerListingReadModel(
                listing.Id,
                listing.Title,
                listing.Description,
                listing.Location,
                listing.NightlyPriceAmount,
                listing.MaxGuests,
                listing.BedroomCount,
                listing.BathroomCount,
                listing.HeroImageUrl,
                listing.Amenities,
                listing.Status.ToString(),
                listing.CreatedAt);
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

    private sealed class FakeReservationRepository : IReservationRepository
    {
        private readonly bool _hasOverlap;

        public FakeReservationRepository(bool hasOverlap)
        {
            _hasOverlap = hasOverlap;
        }

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
            throw new NotSupportedException();
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
