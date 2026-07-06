namespace App.Api.Listings;

internal static class CreateListingRequestValidator
{
    private const decimal MaxNightlyPriceAmount = 100000;

    public static Dictionary<string, string[]> Validate(CreateListingRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors[nameof(request.Title)] = ["Listing title is required."];
        }
        else if (request.Title.Trim().Length > 200)
        {
            errors[nameof(request.Title)] = ["Listing title cannot exceed 200 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors[nameof(request.Description)] = ["Listing description is required."];
        }
        else if (request.Description.Trim().Length > 2000)
        {
            errors[nameof(request.Description)] = ["Listing description cannot exceed 2000 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            errors[nameof(request.Location)] = ["Listing location is required."];
        }
        else if (request.Location.Trim().Length > 160)
        {
            errors[nameof(request.Location)] = ["Listing location cannot exceed 160 characters."];
        }

        if (request.NightlyPriceAmount <= 0 || request.NightlyPriceAmount > MaxNightlyPriceAmount)
        {
            errors[nameof(request.NightlyPriceAmount)] = [$"Nightly price must be greater than zero and no more than {MaxNightlyPriceAmount}."];
        }

        if (request.MaxGuests is < 1 or > 50)
        {
            errors[nameof(request.MaxGuests)] = ["Maximum guests must be between 1 and 50."];
        }

        if (request.BedroomCount is < 0 or > 50)
        {
            errors[nameof(request.BedroomCount)] = ["Bedroom count must be between 0 and 50."];
        }

        if (request.BathroomCount is < 1 or > 50)
        {
            errors[nameof(request.BathroomCount)] = ["Bathroom count must be between 1 and 50."];
        }

        if (!string.IsNullOrWhiteSpace(request.HeroImageUrl))
        {
            var trimmedUrl = request.HeroImageUrl.Trim();

            if (trimmedUrl.Length > 2048 ||
                !Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                errors[nameof(request.HeroImageUrl)] = ["Hero image URL must be a valid http or https URL no longer than 2048 characters."];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Amenities) && request.Amenities.Trim().Length > 500)
        {
            errors[nameof(request.Amenities)] = ["Amenities cannot exceed 500 characters."];
        }

        return errors;
    }
}
