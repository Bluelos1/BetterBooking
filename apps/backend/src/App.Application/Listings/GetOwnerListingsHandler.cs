namespace App.Application.Listings;

public sealed class GetOwnerListingsHandler
{
    private const int MaxPageSize = 50;
    private readonly IListingRepository _listingRepository;

    public GetOwnerListingsHandler(IListingRepository listingRepository)
    {
        _listingRepository = listingRepository;
    }

    public Task<OwnerListingSearchResult> HandleAsync(GetOwnerListingsQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        return _listingRepository.SearchOwnerListingsAsync(query.OwnerUserId, page, pageSize, cancellationToken);
    }
}
