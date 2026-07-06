using System.Security.Claims;
using App.Application.Audit;
using App.Application.Common;

namespace App.Api.Middleware;

public sealed class AuthorizationAuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
        {
            return;
        }

        var auditLog = context.RequestServices.GetService<IAuditLog>();
        var unitOfWork = context.RequestServices.GetService<IApplicationUnitOfWork>();

        if (auditLog is null || unitOfWork is null)
        {
            return;
        }

        var actorUserId = TryGetActorUserId(context.User);

        await auditLog.WriteAsync(new AuditLogEntry(
            AuditEventTypes.AuthorizationFailed,
            actorUserId,
            AuditSubjectTypes.Request,
            null), context.RequestAborted);

        await unitOfWork.SaveChangesAsync(context.RequestAborted);
    }

    private static Guid? TryGetActorUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirst("betterbooking_user_id")?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
