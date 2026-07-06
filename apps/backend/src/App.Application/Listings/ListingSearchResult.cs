namespace App.Application.Listings;

public sealed record ListingSearchResult(
    IReadOnlyList<ListingReadModel> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public bool HasNextPage => Page * PageSize < TotalCount;
}
