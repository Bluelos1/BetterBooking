namespace App.Application.Listings;

public sealed record SearchListingsQuery(
    string? SearchTerm,
    int Page,
    int PageSize);
