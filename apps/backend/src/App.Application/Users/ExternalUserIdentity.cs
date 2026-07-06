namespace App.Application.Users;

public sealed record ExternalUserIdentity(
    string Provider,
    string Subject,
    string? Email,
    string? DisplayName);
