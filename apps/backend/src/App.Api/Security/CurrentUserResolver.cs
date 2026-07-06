using System.Security.Claims;
using App.Application.Users;

namespace App.Api.Security;

public static class CurrentUserResolver
{
    private const string InternalUserIdClaimType = "betterbooking_user_id";

    public static async Task<CurrentUserResolution> ResolveAsync(
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (TryGetInternalUserId(httpContext.User, out var userId))
        {
            return CurrentUserResolution.Success(userId);
        }

        if (!TryGetExternalIdentity(httpContext.User, out var identity))
        {
            return CurrentUserResolution.Forbidden(
                "User mapping required.",
                "The authenticated user does not contain a usable external identity subject.");
        }

        var handler = serviceProvider.GetService<ResolveUserIdentityHandler>();

        if (handler is null)
        {
            return CurrentUserResolution.ServiceUnavailable(
                "User persistence is not configured.",
                "Configure the application database before mapping external users.");
        }

        var resolvedUserId = await handler.HandleAsync(identity, cancellationToken);

        return CurrentUserResolution.Success(resolvedUserId);
    }

    private static bool TryGetInternalUserId(ClaimsPrincipal user, out Guid userId)
    {
        var claimValue = user.FindFirstValue(InternalUserIdClaimType);

        return Guid.TryParse(claimValue, out userId);
    }

    private static bool TryGetExternalIdentity(ClaimsPrincipal user, out ExternalUserIdentity identity)
    {
        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue("oid") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subject))
        {
            identity = new ExternalUserIdentity(string.Empty, string.Empty, null, null);

            return false;
        }

        var provider = user.FindFirstValue("iss") ?? user.Identity?.AuthenticationType ?? "oidc";
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");
        var displayName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue("name");

        identity = new ExternalUserIdentity(provider, subject, email, displayName);

        return true;
    }
}
