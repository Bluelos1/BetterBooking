namespace App.Api.Me;

public sealed record MyReservationsResponse(
    IReadOnlyList<MyReservationResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage);
