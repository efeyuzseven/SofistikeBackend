using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Sofistike.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        using var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
        using var client = application.CreateClient();

        var response = await client.GetAsync("/api/v1/system/health");
        var payload =
            await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", payload?.Status);
    }

    private sealed record HealthResponse(string Status);
}
