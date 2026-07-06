using System.Net;
using System.Net.Http.Json;
using App.Api.Reservations;
using App.ApiTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace App.ApiTests.Reservations;

public sealed class ReservationEndpointTests
{
    [Fact]
    public async Task CreateReservation_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/v1/reservations", CreateValidRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateReservation_WithInvalidRequest_ReturnsValidationProblem()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/v1/reservations", new CreateReservationRequest(
            Guid.Empty,
            new DateOnly(2027, 5, 10),
            new DateOnly(2027, 5, 9)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReservation_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/v1/reservations", CreateValidRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/api/v1/reservations/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_WhenDatabaseIsNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = new WebApplicationFactory<Program>().WithTestAuthentication();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/api/v1/reservations/{Guid.NewGuid()}/cancel", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private static CreateReservationRequest CreateValidRequest()
    {
        return new CreateReservationRequest(
            Guid.NewGuid(),
            new DateOnly(2027, 5, 10),
            new DateOnly(2027, 5, 12));
    }
}
