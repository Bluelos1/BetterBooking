namespace App.Api.Me;

public sealed record MyListingsResponse(
    IReadOnlyList<MyListingResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage);
