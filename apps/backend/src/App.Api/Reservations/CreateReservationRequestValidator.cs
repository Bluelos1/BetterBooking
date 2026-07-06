namespace App.Api.Reservations;

internal static class CreateReservationRequestValidator
{
    public static Dictionary<string, string[]> Validate(CreateReservationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (request.ListingId == Guid.Empty)
        {
            errors[nameof(request.ListingId)] = ["Listing id is required."];
        }

        if (request.EndDate <= request.StartDate)
        {
            errors[nameof(request.EndDate)] = ["End date must be after start date."];
        }

        return errors;
    }
}
