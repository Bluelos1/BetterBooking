using System.Net;
using App.ApiTests.TestSupport;
using Microsoft.AspNetCore.Mvc.Testing;

namespace App.ApiTests.Observability;

public sealed class OpenApiAndHealthEndpointTests
{
    [Fact]
    public async Task OpenApi_InDevelopment_ReturnsDocument()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_WithoutConfiguredDatabase_ReturnsHealthy()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", body, StringComparison.Ordinal);
    }
}
