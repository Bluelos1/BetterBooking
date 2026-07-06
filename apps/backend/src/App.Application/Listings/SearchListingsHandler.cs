namespace App.Application.Listings;

public sealed class SearchListingsHandler
{
    private const int MaxPageSize = 50;
    private readonly IListingRepository _listingRepository;

    public SearchListingsHandler(IListingRepository listingRepository)
    {
        _listingRepository = listingRepository;
    }

    public Task<ListingSearchResult> HandleAsync(SearchListingsQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm) ? null : query.SearchTerm.Trim();

        return _listingRepository.SearchPublishedAsync(searchTerm, page, pageSize, cancellationToken);
    }
}
