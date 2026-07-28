using App.Api.Security;
using App.Application.Listings;

namespace App.Api.Listings;

public static class ListingEndpoints
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;
    private const int MaxSearchTermLength = 100;

    public static IEndpointRouteBuilder MapListingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/listings")
            .WithTags("Listings");

        group.MapGet("", SearchListingsAsync)
            .WithName("SearchListings")
            .AllowAnonymous()
            .Produces<SearchListingsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{listingId:guid}", GetListingAsync)
            .WithName("GetListing")
            .AllowAnonymous()
            .Produces<ListingResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/{listingId:guid}/availability", CheckListingAvailabilityAsync)
            .WithName("CheckListingAvailability")
            .AllowAnonymous()
            .Produces<ListingAvailabilityResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("", CreateListingAsync)
            .WithName("CreateListing")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .RequireRateLimiting(RateLimitPolicies.ListingCreation)
            .Produces<CreateListingResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/{listingId:guid}/publish", PublishListingAsync)
            .WithName("PublishListing")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .Produces<CreateListingResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/{listingId:guid}/unpublish", UnpublishListingAsync)
            .WithName("UnpublishListing")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .Produces<CreateListingResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/{listingId:guid}/archive", ArchiveListingAsync)
            .WithName("ArchiveListing")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .Produces<CreateListingResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> SearchListingsAsync(
        string? q,
        int? page,
        int? pageSize,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var requestedPage = page ?? DefaultPage;
        var requestedPageSize = pageSize ?? DefaultPageSize;
        var validationErrors = ValidateSearchQuery(q, requestedPage, requestedPageSize);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var handler = serviceProvider.GetService<SearchListingsHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Listing persistence is not configured.",
                detail: "Configure the application database before searching listings.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new SearchListingsQuery(q, requestedPage, requestedPageSize), cancellationToken);

        return Results.Ok(new SearchListingsResponse(
            result.Items.Select(ToResponse).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.HasNextPage));
    }

    private static async Task<IResult> GetListingAsync(
        Guid listingId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetService<GetListingHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Listing persistence is not configured.",
                detail: "Configure the application database before reading listings.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var listing = await handler.HandleAsync(listingId, cancellationToken);

        return listing is null ? Results.NotFound() : Results.Ok(ToResponse(listing));
    }

    private static async Task<IResult> CheckListingAvailabilityAsync(
        Guid listingId,
        string? startDate,
        string? endDate,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateAvailabilityQuery(startDate, endDate, out var parsedStartDate, out var parsedEndDate);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var handler = serviceProvider.GetService<CheckListingAvailabilityHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Listing persistence is not configured.",
                detail: "Configure the application database before checking listing availability.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new CheckListingAvailabilityQuery(
            listingId,
            parsedStartDate,
            parsedEndDate), cancellationToken);

        if (result.Succeeded && result.Available is not null)
        {
            return Results.Ok(new ListingAvailabilityResponse(
                result.ListingId,
                result.StartDate,
                result.EndDate,
                result.Available.Value));
        }

        return result.Error switch
        {
            ListingAvailabilityError.ListingNotFound => Results.NotFound(),
            ListingAvailabilityError.ValidationFailed => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [result.Detail ?? "Availability request is invalid."]
            }),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<IResult> CreateListingAsync(
        CreateListingRequest request,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var validationErrors = CreateListingRequestValidator.Validate(request);

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

        var handler = serviceProvider.GetService<CreateListingHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Listing persistence is not configured.",
                detail: "Configure the application database before creating listings.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new CreateListingCommand(
            Guid.NewGuid(),
            currentUser.UserId.Value,
            request.Title,
            request.Description,
            request.Location,
            request.NightlyPriceAmount,
            request.MaxGuests,
            request.BedroomCount,
            request.BathroomCount,
            request.HeroImageUrl,
            request.Amenities), cancellationToken);

        if (result.Succeeded && result.ListingId is not null)
        {
            return Results.Created(
                $"/api/v1/listings/{result.ListingId}",
                new CreateListingResponse(result.ListingId.Value, "Draft"));
        }

        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = [result.Detail ?? "Listing request is invalid."]
        });
    }

    private static async Task<IResult> PublishListingAsync(
        Guid listingId,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var currentUser = await CurrentUserResolver.ResolveAsync(httpContext, serviceProvider, cancellationToken);

        if (!currentUser.Succeeded || currentUser.UserId is null)
        {
            return Results.Problem(
                title: currentUser.FailureTitle,
                detail: currentUser.FailureDetail,
                statusCode: currentUser.FailureStatusCode);
        }

        var handler = serviceProvider.GetService<PublishListingHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Listing persistence is not configured.",
                detail: "Configure the application database before publishing listings.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new PublishListingCommand(
            listingId,
            currentUser.UserId.Value), cancellationToken);

        if (result.Succeeded && result.ListingId is not null)
        {
            return Results.Ok(new CreateListingResponse(result.ListingId.Value, "Published"));
        }

        return result.Error switch
        {
            PublishListingError.NotFound => Results.NotFound(),
            PublishListingError.Forbidden => Results.Problem(
                title: "Forbidden.",
                detail: result.Detail,
                statusCode: StatusCodes.Status403Forbidden),
            PublishListingError.InvalidState => Results.Problem(
                title: "Listing state conflict.",
                detail: result.Detail,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<IResult> UnpublishListingAsync(
        Guid listingId,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var currentUser = await CurrentUserResolver.ResolveAsync(httpContext, serviceProvider, cancellationToken);

        if (!currentUser.Succeeded || currentUser.UserId is null)
        {
            return Results.Problem(
                title: currentUser.FailureTitle,
                detail: currentUser.FailureDetail,
                statusCode: currentUser.FailureStatusCode);
        }

        var handler = serviceProvider.GetService<UnpublishListingHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Listing persistence is not configured.",
                detail: "Configure the application database before unpublishing listings.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new UpdateListingStatusCommand(
            listingId,
            currentUser.UserId.Value), cancellationToken);

        return ToListingStatusResult(result);
    }

    private static async Task<IResult> ArchiveListingAsync(
        Guid listingId,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var currentUser = await CurrentUserResolver.ResolveAsync(httpContext, serviceProvider, cancellationToken);

        if (!currentUser.Succeeded || currentUser.UserId is null)
        {
            return Results.Problem(
                title: currentUser.FailureTitle,
                detail: currentUser.FailureDetail,
                statusCode: currentUser.FailureStatusCode);
        }

        var handler = serviceProvider.GetService<ArchiveListingHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Listing persistence is not configured.",
                detail: "Configure the application database before archiving listings.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new UpdateListingStatusCommand(
            listingId,
            currentUser.UserId.Value), cancellationToken);

        return ToListingStatusResult(result);
    }

    private static Dictionary<string, string[]> ValidateSearchQuery(string? q, int page, int pageSize)
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

        if (q is not null && q.Trim().Length > MaxSearchTermLength)
        {
            errors["q"] = [$"Search term must be {MaxSearchTermLength} characters or fewer."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateAvailabilityQuery(
        string? startDate,
        string? endDate,
        out DateOnly parsedStartDate,
        out DateOnly parsedEndDate)
    {
        var errors = new Dictionary<string, string[]>();
        var hasStartDate = DateOnly.TryParse(startDate, out parsedStartDate);
        var hasEndDate = DateOnly.TryParse(endDate, out parsedEndDate);

        if (!hasStartDate)
        {
            errors["startDate"] = ["Start date must be a valid date."];
        }

        if (!hasEndDate)
        {
            errors["endDate"] = ["End date must be a valid date."];
        }

        if (hasStartDate && hasEndDate && parsedEndDate <= parsedStartDate)
        {
            errors["endDate"] = ["End date must be after start date."];
        }

        return errors;
    }

    private static ListingResponse ToResponse(ListingReadModel listing)
    {
        return new ListingResponse(
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
            listing.CreatedAt);
    }

    private static IResult ToListingStatusResult(UpdateListingStatusResult result)
    {
        if (result.Succeeded && result.ListingId is not null && result.Status is not null)
        {
            return Results.Ok(new CreateListingResponse(result.ListingId.Value, result.Status));
        }

        return result.Error switch
        {
            UpdateListingStatusError.NotFound => Results.NotFound(),
            UpdateListingStatusError.Forbidden => Results.Problem(
                title: "Forbidden.",
                detail: result.Detail,
                statusCode: StatusCodes.Status403Forbidden),
            UpdateListingStatusError.InvalidState => Results.Problem(
                title: "Listing state conflict.",
                detail: result.Detail,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
