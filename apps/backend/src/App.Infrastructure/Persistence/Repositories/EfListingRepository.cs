using App.Application.Listings;
using App.Domain.Listings;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence.Repositories;

public sealed class EfListingRepository : IListingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfListingRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Listings.FirstOrDefaultAsync(listing => listing.Id == id, cancellationToken);
    }

    public Task<ListingReadModel?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.Id == id && listing.Status == ListingStatus.Published)
            .Select(listing => new ListingReadModel(
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
                listing.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Listings
            .AsNoTracking()
            .AnyAsync(listing => listing.Id == id && listing.Status == ListingStatus.Published, cancellationToken);
    }

    public async Task<ListingSearchResult> SearchPublishedAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.Status == ListingStatus.Published);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{EscapeLikePattern(searchTerm)}%";
            query = query.Where(listing =>
                EF.Functions.ILike(listing.Title, pattern, "\\") ||
                EF.Functions.ILike(listing.Location, pattern, "\\") ||
                EF.Functions.ILike(listing.Description, pattern, "\\"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(listing => listing.CreatedAt)
            .ThenBy(listing => listing.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(listing => new ListingReadModel(
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
                listing.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ListingSearchResult(items, page, pageSize, totalCount);
    }

    public async Task<OwnerListingSearchResult> SearchOwnerListingsAsync(
        Guid ownerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Listings
            .AsNoTracking()
            .Where(listing => listing.OwnerUserId == ownerUserId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(listing => listing.CreatedAt)
            .ThenBy(listing => listing.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(listing => new OwnerListingReadModel(
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
                listing.CreatedAt))
            .ToListAsync(cancellationToken);

        return new OwnerListingSearchResult(items, page, pageSize, totalCount);
    }

    public async Task AddAsync(Listing listing, CancellationToken cancellationToken)
    {
        await _dbContext.Listings.AddAsync(listing, cancellationToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
