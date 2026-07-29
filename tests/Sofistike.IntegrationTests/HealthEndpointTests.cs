using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sofistike.IntegrationTests;

public sealed class HealthEndpointTests(
    WebApplicationFactory<Program> application
) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        using var client = application.CreateClient();

        var response = await client.GetAsync("/api/v1/system/health");
        var payload =
            await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", payload?.Status);
    }

    private sealed record HealthResponse(string Status);
}
