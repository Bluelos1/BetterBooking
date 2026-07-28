using App.Api.Middleware;
using App.Api.Listings;
using App.Api.Me;
using App.Api.Observability;
using App.Api.Reservations;
using App.Api.Security;
using App.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddApplicationTelemetry(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddApiAuthenticationAndAuthorization(builder.Configuration, builder.Environment);
builder.Services.AddInfrastructure(builder.Configuration);

var defaultRequestTimeoutSeconds = builder.Configuration.GetValue<int?>("RequestTimeouts:DefaultSeconds") ?? 30;

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(defaultRequestTimeoutSeconds),
        TimeoutStatusCode = StatusCodes.Status503ServiceUnavailable
    };
});

var reservationRateLimitPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:ReservationCreation:PermitLimit") ?? 20;
var reservationRateLimitWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:ReservationCreation:WindowSeconds") ?? 60;
var listingRateLimitPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:ListingCreation:PermitLimit") ?? 10;
var listingRateLimitWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:ListingCreation:WindowSeconds") ?? 3600;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.ReservationCreation, httpContext =>
    {
        var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            httpContext.Connection.RemoteIpAddress?.ToString() ??
            "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = reservationRateLimitPermitLimit,
            QueueLimit = 0,
            Window = TimeSpan.FromSeconds(reservationRateLimitWindowSeconds)
        });
    });

    options.AddPolicy(RateLimitPolicies.ListingCreation, httpContext =>
    {
        var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            httpContext.Connection.RemoteIpAddress?.ToString() ??
            "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            PermitLimit = listingRateLimitPermitLimit,
            QueueLimit = 0,
            Window = TimeSpan.FromSeconds(listingRateLimitWindowSeconds)
        });
    });
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(section => section.Value)
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .Select(value => value!)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredFrontendOrigins", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSecurityHeaders();
app.UseCors("ConfiguredFrontendOrigins");
app.UseRequestTimeouts();
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<AuthorizationAuditMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponseAsync
});

var api = app.MapGroup("/api/v1");

api.MapGet("/system/status", () => Results.Ok(new
{
    service = "BetterBooking API",
    status = "ok",
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("GetSystemStatus");

app.MapReservationEndpoints();
app.MapListingEndpoints();
app.MapMeEndpoints();

app.Run();

static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            durationMilliseconds = entry.Value.Duration.TotalMilliseconds
        })
    };

    return context.Response.WriteAsJsonAsync(response);
}

public partial class Program;
