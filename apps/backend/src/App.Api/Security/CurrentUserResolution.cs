namespace App.Api.Security;

public sealed record CurrentUserResolution(
    bool Succeeded,
    Guid? UserId,
    int? FailureStatusCode,
    string? FailureTitle,
    string? FailureDetail)
{
    public static CurrentUserResolution Success(Guid userId) => new(true, userId, null, null, null);

    public static CurrentUserResolution Forbidden(string title, string detail) => new(
        false,
        null,
        StatusCodes.Status403Forbidden,
        title,
        detail);

    public static CurrentUserResolution ServiceUnavailable(string title, string detail) => new(
        false,
        null,
        StatusCodes.Status503ServiceUnavailable,
        title,
        detail);
}
