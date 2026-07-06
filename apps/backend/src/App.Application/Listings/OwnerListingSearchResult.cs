namespace App.Application.Listings;

public sealed record OwnerListingSearchResult(
    IReadOnlyList<OwnerListingReadModel> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public bool HasNextPage => Page * PageSize < TotalCount;
}
