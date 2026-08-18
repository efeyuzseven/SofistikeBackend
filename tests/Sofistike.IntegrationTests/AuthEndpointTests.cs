using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sofistike.Application.Authentication;
using Sofistike.Application.Users;

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

    [Fact]
    public async Task RegisterCreatesUserAndSession()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                Email = "yeni@sofistike.com",
                FirstName = "Yeni",
                LastName = "Kullanıcı",
                Password = "Guclu123!",
            }
        );
        var registration = await response.Content
            .ReadFromJsonAsync<LoginPayload>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(registration);
        Assert.Equal("yeni@sofistike.com", registration.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(registration.AccessToken));
    }

    [Fact]
    public async Task RegisterRejectsExistingEmail()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                Email = "umay@sofistike.com",
                FirstName = "Umay",
                LastName = "",
                Password = "Guclu123!",
            }
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RegisterRejectsWeakPassword()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new
            {
                Email = "yeni@sofistike.com",
                FirstName = "Yeni",
                LastName = "Kullanıcı",
                Password = "zayifsifre",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProfileCanBeReadAndUpdated()
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
        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/v1/account/profile",
            new
            {
                FirstName = "Umay",
                LastName = "Yılmaz",
                PhoneNumber = "+90 555 123 45 67",
            }
        );
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<ProfilePayload>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Yılmaz", updated.LastName);
        Assert.Equal("+90 555 123 45 67", updated.PhoneNumber);

        var profile = await client.GetFromJsonAsync<ProfilePayload>(
            "/api/v1/account/profile"
        );

        Assert.Equal(updated, profile);
    }

    [Fact]
    public async Task ProfileRequiresAuthentication()
    {
        using var application = CreateApplication();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/api/v1/account/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private sealed record ProfilePayload(
        Guid Id,
        string Email,
        string FirstName,
        string? LastName,
        string? PhoneNumber,
        string Role
    );

    private static WebApplicationFactory<Program> CreateApplication()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("Catalog:SeedDevelopmentData", "false");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ICredentialValidator>();
                    services.AddSingleton<ICredentialValidator,
                        TestCredentialValidator>();
                    services.RemoveAll<IUserRegistrationService>();
                    services.AddSingleton<IUserRegistrationService,
                        TestUserRegistrationService>();
                    services.RemoveAll<IUserProfileService>();
                    services.AddSingleton<IUserProfileService,
                        TestUserProfileService>();
                });
            }
        );
    }

    private sealed class TestCredentialValidator : ICredentialValidator
    {
        public Task<AuthenticatedUser?> ValidateAsync(
            LoginCommand command,
            CancellationToken cancellationToken = default
        )
        {
            var matches = string.Equals(
                    command.Email.Trim(),
                    "umay@sofistike.com",
                    StringComparison.OrdinalIgnoreCase
                )
                && command.Password == "Umay123!";

            AuthenticatedUser? user = matches
                ? new AuthenticatedUser(
                    Guid.Parse("d8fbd714-b22f-4a7f-b576-c6a2183f6e80"),
                    "umay@sofistike.com",
                    "Umay",
                    "Customer"
                )
                : null;

            return Task.FromResult(user);
        }
    }

    private sealed class TestUserRegistrationService
        : IUserRegistrationService
    {
        public Task<UserRegistrationResult> RegisterAsync(
            RegisterUserCommand command,
            CancellationToken cancellationToken = default
        )
        {
            if (string.Equals(
                    command.Email.Trim(),
                    "umay@sofistike.com",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return Task.FromResult(
                    new UserRegistrationResult(
                        UserRegistrationStatus.EmailAlreadyExists,
                        null
                    )
                );
            }

            var user = new AuthenticatedUser(
                Guid.NewGuid(),
                command.Email.Trim(),
                command.FirstName.Trim(),
                "Customer"
            );

            return Task.FromResult(
                new UserRegistrationResult(
                    UserRegistrationStatus.Created,
                    user
                )
            );
        }
    }

    private sealed class TestUserProfileService : IUserProfileService
    {
        private UserProfile _profile = new(
            Guid.Parse("d8fbd714-b22f-4a7f-b576-c6a2183f6e80"),
            "umay@sofistike.com",
            "Umay",
            null,
            null,
            "Customer"
        );

        public Task<UserProfile?> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<UserProfile?>(
                userId == _profile.Id ? _profile : null
            );
        }

        public Task<UserProfile?> UpdateAsync(
            UpdateUserProfileCommand command,
            CancellationToken cancellationToken = default
        )
        {
            if (command.UserId != _profile.Id)
            {
                return Task.FromResult<UserProfile?>(null);
            }

            _profile = _profile with
            {
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName?.Trim(),
                PhoneNumber = command.PhoneNumber?.Trim(),
            };

            return Task.FromResult<UserProfile?>(_profile);
        }
    }
}
