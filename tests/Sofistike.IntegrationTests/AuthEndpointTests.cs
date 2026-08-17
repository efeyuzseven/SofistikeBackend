using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Sofistike.IntegrationTests;

public sealed class AuthEndpointTests
{
    [Fact]
    public async Task LoginAndMeReturnDevelopmentUser()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                Email = "umay@sofistike.com",
                Password = "Umay123!",
                RememberMe = false,
            }
        );
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginPayload>();

        Assert.True(
            loginResponse.StatusCode == HttpStatusCode.OK,
            await loginResponse.Content.ReadAsStringAsync()
        );
        Assert.NotNull(login);
        Assert.Equal("umay@sofistike.com", login.User.Email);
        Assert.Equal("Umay", login.User.FirstName);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        var user = await meResponse.Content.ReadFromJsonAsync<UserPayload>();

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.Equal(login.User, user);
    }

    [Fact]
    public async Task LoginRejectsInvalidCredentials()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                Email = "umay@sofistike.com",
                Password = "yanlis-sifre",
                RememberMe = false,
            }
        );

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized,
            await response.Content.ReadAsStringAsync()
        );
    }

    private sealed record LoginPayload(
        UserPayload User,
        string AccessToken,
        int ExpiresIn
    );

    private sealed record UserPayload(
        Guid Id,
        string Email,
        string FirstName,
        string Role
    );

    private static WebApplicationFactory<Program> CreateApplication()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
            }
        );
    }
}
