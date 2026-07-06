namespace App.Domain.Users;

public sealed class User
{
    private User()
    {
        ExternalProvider = string.Empty;
        ExternalSubject = string.Empty;
    }

    private User(
        Guid id,
        string externalProvider,
        string externalSubject,
        string? email,
        string? displayName,
        DateTimeOffset createdAt)
    {
        Id = id;
        ExternalProvider = externalProvider;
        ExternalSubject = externalSubject;
        Email = email;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string ExternalProvider { get; private set; }

    public string ExternalSubject { get; private set; }

    public string? Email { get; private set; }

    public string? DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static User Create(
        Guid id,
        string externalProvider,
        string externalSubject,
        string? email,
        string? displayName,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(externalProvider))
        {
            throw new ArgumentException("External provider is required.", nameof(externalProvider));
        }

        if (string.IsNullOrWhiteSpace(externalSubject))
        {
            throw new ArgumentException("External subject is required.", nameof(externalSubject));
        }

        return new User(
            id,
            externalProvider.Trim(),
            externalSubject.Trim(),
            NormalizeOptional(email),
            NormalizeOptional(displayName),
            createdAt);
    }

    public void UpdateProfileHints(string? email, string? displayName, DateTimeOffset updatedAt)
    {
        Email = NormalizeOptional(email);
        DisplayName = NormalizeOptional(displayName);
        UpdatedAt = updatedAt;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
