using App.Api.Security;
using App.Application.Reservations;

namespace App.Api.Reservations;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reservations")
            .WithTags("Reservations")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapPost("", CreateReservationAsync)
            .WithName("CreateReservation")
            .RequireRateLimiting(RateLimitPolicies.ReservationCreation)
            .Produces<CreateReservationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/{reservationId:guid}/cancel", CancelReservationAsync)
            .WithName("CancelReservation")
            .Produces<CancelReservationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost("/{reservationId:guid}/payment/confirm", ConfirmPaymentAsync)
            .WithName("ConfirmReservationPayment")
            .Produces<ConfirmReservationPaymentResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> CreateReservationAsync(
        CreateReservationRequest request,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var validationErrors = CreateReservationRequestValidator.Validate(request);

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

        var handler = serviceProvider.GetService<CreateReservationHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Reservation persistence is not configured.",
                detail: "Configure the application database before creating reservations.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new CreateReservationCommand(
            Guid.NewGuid(),
            request.ListingId,
            currentUser.UserId.Value,
            request.StartDate,
            request.EndDate), cancellationToken);

        if (result.Succeeded && result.ReservationId is not null)
        {
            return Results.Created(
                $"/api/v1/reservations/{result.ReservationId}",
                new CreateReservationResponse(result.ReservationId.Value, "Pending", "Unpaid"));
        }

        return result.Error switch
        {
            CreateReservationError.ValidationFailed => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [result.Detail ?? "Reservation request is invalid."]
            }),
            CreateReservationError.ListingUnavailable => Results.Problem(
                title: "Listing unavailable.",
                detail: result.Detail,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<IResult> CancelReservationAsync(
        Guid reservationId,
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

        var handler = serviceProvider.GetService<CancelReservationHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Reservation persistence is not configured.",
                detail: "Configure the application database before cancelling reservations.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new CancelReservationCommand(
            reservationId,
            currentUser.UserId.Value), cancellationToken);

        if (result.Succeeded && result.ReservationId is not null && result.PaymentStatus is not null)
        {
            return Results.Ok(new CancelReservationResponse(result.ReservationId.Value, "Cancelled", result.PaymentStatus));
        }

        return result.Error switch
        {
            CancelReservationError.ValidationFailed => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [result.Detail ?? "Cancellation request is invalid."]
            }),
            CancelReservationError.NotFound => Results.NotFound(),
            CancelReservationError.Forbidden => Results.Problem(
                title: "Forbidden.",
                detail: result.Detail,
                statusCode: StatusCodes.Status403Forbidden),
            CancelReservationError.InvalidState => Results.Problem(
                title: "Reservation state conflict.",
                detail: result.Detail,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<IResult> ConfirmPaymentAsync(
        Guid reservationId,
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

        var handler = serviceProvider.GetService<ConfirmReservationPaymentHandler>();

        if (handler is null)
        {
            return Results.Problem(
                title: "Reservation persistence is not configured.",
                detail: "Configure the application database before confirming reservation payments.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await handler.HandleAsync(new ConfirmReservationPaymentCommand(
            reservationId,
            currentUser.UserId.Value), cancellationToken);

        if (result.Succeeded && result.ReservationId is not null && result.Status is not null && result.PaymentStatus is not null)
        {
            return Results.Ok(new ConfirmReservationPaymentResponse(
                result.ReservationId.Value,
                result.Status,
                result.PaymentStatus));
        }

        return result.Error switch
        {
            ConfirmReservationPaymentError.ValidationFailed => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [result.Detail ?? "Payment confirmation request is invalid."]
            }),
            ConfirmReservationPaymentError.NotFound => Results.NotFound(),
            ConfirmReservationPaymentError.Forbidden => Results.Problem(
                title: "Forbidden.",
                detail: result.Detail,
                statusCode: StatusCodes.Status403Forbidden),
            ConfirmReservationPaymentError.InvalidState => Results.Problem(
                title: "Reservation payment state conflict.",
                detail: result.Detail,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

}
