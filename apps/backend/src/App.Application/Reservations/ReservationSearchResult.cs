namespace App.Application.Reservations;

public sealed record ReservationSearchResult(
    IReadOnlyList<ReservationReadModel> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public bool HasNextPage => Page * PageSize < TotalCount;
}
