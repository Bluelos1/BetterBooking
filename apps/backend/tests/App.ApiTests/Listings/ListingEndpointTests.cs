using System.Net;
using System.Net.Http.Json;
using App.Api.Listings;
using App.ApiTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace App.ApiTests.Listings;

public sealed class ListingEndpointTests
{
    [Fact]
    public async Task SearchListings_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/listings");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task SearchListings_WithInvalidPageSize_ReturnsValidationProblem()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/listings?pageSize=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetListing_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/api/v1/listings/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CheckAvailability_WithInvalidDates_ReturnsValidationProblem()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/listings/{Guid.NewGuid()}/availability?startDate=2027-05-10&endDate=2027-05-09");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CheckAvailability_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync(
            $"/api/v1/listings/{Guid.NewGuid()}/availability?startDate=2027-05-10&endDate=2027-05-12");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/v1/listings", CreateValidRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_WithInvalidRequest_ReturnsValidationProblem()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/v1/listings", CreateValidRequest() with { Title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/v1/listings", CreateValidRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task PublishListing_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/api/v1/listings/{Guid.NewGuid()}/publish", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task UnpublishListing_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/api/v1/listings/{Guid.NewGuid()}/unpublish", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnpublishListing_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/api/v1/listings/{Guid.NewGuid()}/unpublish", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task ArchiveListing_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/api/v1/listings/{Guid.NewGuid()}/archive", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ArchiveListing_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/api/v1/listings/{Guid.NewGuid()}/archive", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static CreateListingRequest CreateValidRequest()
    {
        return new CreateListingRequest(
            "City apartment",
            "Bright apartment close to transit with a quiet workspace.",
            "Krakow, Old Town",
            180m,
            4,
            2,
            1,
            null,
            "Wi-Fi, Kitchen, Workspace");
    }
}
