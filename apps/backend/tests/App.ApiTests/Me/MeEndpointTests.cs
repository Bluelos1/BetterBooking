using System.Net;
using App.ApiTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace App.ApiTests.Me;

public sealed class MeEndpointTests
{
    [Fact]
    public async Task GetMyListings_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/me/listings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyListings_WithInvalidPagination_ReturnsValidationProblem()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/me/listings?pageSize=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyListings_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/me/listings");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task GetMyReservations_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/me/reservations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyReservations_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/v1/me/reservations");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
