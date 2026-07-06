using App.Api.Security;
using App.Application.Listings;
using App.Application.Reservations;

namespace App.Api.Me;

public static class MeEndpoints
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/me")
            .WithTags("Me")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapGet("/listings", GetMyListingsAsync)
            .WithName("GetMyListings")
            .RequireAuthorization(AuthorizationPolicies.PropertyAdmin)
            .Produces<MyListingsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/reservations", GetMyReservationsAsync)
            .WithName("GetMyReservations")
            .Produces<MyReservationsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> GetMyListingsAsync(
        int? page,
        int? pageSize,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var requestedPage = page ?? DefaultPage;
        var requestedPageSize = pageSize ?? DefaultPageSize;
        var validationErrors = ValidatePagination(requestedPage, requestedPageSize);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var currentUser = await CurrentUserResolver.ResolveAsync(httpContext, serviceProvider, cancellationToken);

        if (!currentUser.Succeeded || currentUser.UserId is null)
        {
            return Results.Problem(
                title: currentUser.FailureTitle,
                detail: currentUser.FailureDetail,
                statusCode: currentUser.FailureStatusCode);
        }

        var handler = serviceProvider.GetService<GetOwnerListingsHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Listing persistence is not configured.",
                detail: "Configure the application database before reading user listings.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new GetOwnerListingsQuery(
            currentUser.UserId.Value,
            requestedPage,
            requestedPageSize), cancellationToken);

        return Results.Ok(new MyListingsResponse(
            result.Items.Select(ToResponse).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.HasNextPage));
    }

    private static async Task<IResult> GetMyReservationsAsync(
        int? page,
        int? pageSize,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var requestedPage = page ?? DefaultPage;
        var requestedPageSize = pageSize ?? DefaultPageSize;
        var validationErrors = ValidatePagination(requestedPage, requestedPageSize);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var currentUser = await CurrentUserResolver.ResolveAsync(httpContext, serviceProvider, cancellationToken);

        if (!currentUser.Succeeded || currentUser.UserId is null)
        {
            return Results.Problem(
                title: currentUser.FailureTitle,
                detail: currentUser.FailureDetail,
                statusCode: currentUser.FailureStatusCode);
        }

        var handler = serviceProvider.GetService<GetGuestReservationsHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Reservation persistence is not configured.",
                detail: "Configure the application database before reading user reservations.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new GetGuestReservationsQuery(
            currentUser.UserId.Value,
            requestedPage,
            requestedPageSize), cancellationToken);

        return Results.Ok(new MyReservationsResponse(
            result.Items.Select(ToResponse).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.HasNextPage));
    }

    private static Dictionary<string, string[]> ValidatePagination(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();

        if (page < 1)
        {
            errors["page"] = ["Page must be greater than or equal to 1."];
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            errors["pageSize"] = [$"Page size must be between 1 and {MaxPageSize}."];
        }

        return errors;
    }

    private static MyListingResponse ToResponse(OwnerListingReadModel listing)
    {
        return new MyListingResponse(
            listing.Id,
            listing.Title,
            listing.Description,
            listing.Location,
            listing.NightlyPriceAmount,
            listing.MaxGuests,
            listing.BedroomCount,
            listing.BathroomCount,
            listing.HeroImageUrl,
            listing.Amenities,
            listing.Status,
            listing.CreatedAt);
    }

    private static MyReservationResponse ToResponse(ReservationReadModel reservation)
    {
        return new MyReservationResponse(
            reservation.Id,
            reservation.ListingId,
            reservation.ListingTitle,
            reservation.StartDate,
            reservation.EndDate,
            reservation.Status,
            reservation.PaymentStatus,
            reservation.CreatedAt,
            reservation.UpdatedAt);
    }
}
