namespace App.Api.Listings;

public sealed record SearchListingsResponse(
    IReadOnlyList<ListingResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage);
