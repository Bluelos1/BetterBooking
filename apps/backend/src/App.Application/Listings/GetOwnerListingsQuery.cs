namespace App.Application.Listings;

public sealed record GetOwnerListingsQuery(
    Guid OwnerUserId,
    int Page,
    int PageSize);
