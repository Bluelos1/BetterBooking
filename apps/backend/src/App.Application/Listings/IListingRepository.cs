using App.Domain.Listings;

namespace App.Application.Listings;

public interface IListingRepository
{
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ListingReadModel?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsPublishedAsync(Guid id, CancellationToken cancellationToken);

    Task<ListingSearchResult> SearchPublishedAsync(string? searchTerm, int page, int pageSize, CancellationToken cancellationToken);

    Task<OwnerListingSearchResult> SearchOwnerListingsAsync(Guid ownerUserId, int page, int pageSize, CancellationToken cancellationToken);

    Task AddAsync(Listing listing, CancellationToken cancellationToken);
}
