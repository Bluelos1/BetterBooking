namespace App.Application.Listings;

public sealed class GetListingHandler
{
    private readonly IListingRepository _listingRepository;

    public GetListingHandler(IListingRepository listingRepository)
    {
        _listingRepository = listingRepository;
    }

    public Task<ListingReadModel?> HandleAsync(Guid listingId, CancellationToken cancellationToken)
    {
        return _listingRepository.GetPublishedByIdAsync(listingId, cancellationToken);
    }
}
