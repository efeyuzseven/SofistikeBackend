using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sofistike.Application.Authentication;
using Sofistike.Application.Content;

namespace Sofistike.IntegrationTests;

public sealed class BannerEndpointTests
{
    private static readonly Guid BannerId =
        Guid.Parse("5f114324-b407-4be1-a249-d7dcf75c33c1");

    [Fact]
    public async Task PublicEndpointReturnsActiveBanners()
    {
        var service = new TestHomeBannerService();
        using var application = CreateApplication(service, "Customer");
        using var client = application.CreateClient();

        var response = await client.GetAsync("/api/v1/content/banners");
        var banners = await response.Content.ReadFromJsonAsync<
            List<HomeBannerDetails>
        >();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(banners ?? []);
        Assert.Equal(1, service.ActiveRequests);
    }

    [Fact]
    public async Task BannerManagementRequiresAdministratorRole()
    {
        var service = new TestHomeBannerService();
        using var application = CreateApplication(service, "Customer");
        using var client = application.CreateClient();

        var anonymousResponse = await client.GetAsync("/api/v1/admin/banners");
        await Authenticate(client);
        var customerResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/banners",
            ValidBannerRequest()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, customerResponse.StatusCode);
        Assert.Null(service.LastCommand);
    }

    [Fact]
    public async Task AdministratorCanCreateUpdateAndDeleteBanner()
    {
        var service = new TestHomeBannerService();
        using var application = CreateApplication(service, "Admin");
        using var client = application.CreateClient();
        await Authenticate(client);

        var listResponse = await client.GetAsync("/api/v1/admin/banners");
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/banners",
            ValidBannerRequest()
        );
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/banners/{BannerId}",
            ValidBannerRequest()
        );
        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/admin/banners/{BannerId}"
        );

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal("Yeni sezon", service.LastCommand?.Title);
        Assert.Equal(BannerId, service.LastUpdatedId);
        Assert.Equal(BannerId, service.LastDeletedId);
    }

    [Fact]
    public async Task BannerRejectsUnsafeImageAddress()
    {
        var service = new TestHomeBannerService();
        using var application = CreateApplication(service, "Admin");
        using var client = application.CreateClient();
        await Authenticate(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/banners",
            new
            {
                ImageUrl = "javascript:alert(1)",
                AltText = "Geçersiz banner görseli",
                DisplayOrder = 1,
                IsActive = true,
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.LastCommand);
    }

    private static object ValidBannerRequest() => new
    {
        ImageUrl = "/images/hero-home.png",
        AltText = "Yeni sezon ev yaşam koleksiyonu",
        Title = "Yeni sezon",
        Description = "Sade ve uzun ömürlü ev yaşam çözümleri.",
        ButtonText = "Keşfet",
        LinkUrl = "/#moduller",
        DisplayOrder = 2,
        IsActive = true,
    };

    private static async Task Authenticate(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                Email = "admin@sofistike.com",
                Password = "Admin123!",
                RememberMe = false,
            }
        );
        var login = await response.Content.ReadFromJsonAsync<LoginPayload>();
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    private static WebApplicationFactory<Program> CreateApplication(
        IHomeBannerService homeBannerService,
        string role
    )
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Catalog:SeedDevelopmentData", "false");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICredentialValidator>();
                services.AddSingleton<ICredentialValidator>(
                    new TestCredentialValidator(role)
                );
                services.RemoveAll<IHomeBannerService>();
                services.AddSingleton(homeBannerService);
            });
        });
    }

    private sealed record LoginPayload(string AccessToken);

    private sealed class TestCredentialValidator(string role)
        : ICredentialValidator
    {
        public Task<AuthenticatedUser?> ValidateAsync(
            LoginCommand command,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<AuthenticatedUser?>(new AuthenticatedUser(
            Guid.Parse("f9d85ed2-077b-4ae0-a248-9fa49d316829"),
            command.Email,
            "Admin",
            role
        ));
    }

    private sealed class TestHomeBannerService : IHomeBannerService
    {
        public int ActiveRequests { get; private set; }
        public SaveHomeBannerCommand? LastCommand { get; private set; }
        public Guid? LastUpdatedId { get; private set; }
        public Guid? LastDeletedId { get; private set; }

        public Task<IReadOnlyList<HomeBannerDetails>> GetActiveAsync(
            CancellationToken cancellationToken = default
        )
        {
            ActiveRequests++;
            return Task.FromResult<IReadOnlyList<HomeBannerDetails>>([Banner()]);
        }

        public Task<IReadOnlyList<HomeBannerDetails>> GetAllAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<HomeBannerDetails>>([Banner()]);

        public Task<HomeBannerDetails> CreateAsync(
            SaveHomeBannerCommand command,
            CancellationToken cancellationToken = default
        )
        {
            LastCommand = command;
            return Task.FromResult(Banner());
        }

        public Task<HomeBannerDetails?> UpdateAsync(
            Guid bannerId,
            SaveHomeBannerCommand command,
            CancellationToken cancellationToken = default
        )
        {
            LastUpdatedId = bannerId;
            LastCommand = command;
            return Task.FromResult<HomeBannerDetails?>(Banner());
        }

        public Task<bool> DeleteAsync(
            Guid bannerId,
            CancellationToken cancellationToken = default
        )
        {
            LastDeletedId = bannerId;
            return Task.FromResult(true);
        }

        private static HomeBannerDetails Banner() => new(
            BannerId,
            "/images/hero-home.png",
            "Ev yaşam koleksiyonu",
            "Yeni sezon",
            null,
            null,
            null,
            1,
            true,
            DateTimeOffset.UtcNow
        );
    }
}
