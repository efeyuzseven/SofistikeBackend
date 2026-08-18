using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sofistike.Application.Authentication;
using Sofistike.Application.Catalog;
using Sofistike.Application.Favorites;

namespace Sofistike.IntegrationTests;

public sealed class FavoriteEndpointTests
{
    private static readonly Guid UserId =
        Guid.Parse("d8fbd714-b22f-4a7f-b576-c6a2183f6e80");
    private static readonly Guid ProductId =
        Guid.Parse("3310ead5-3459-43a7-982f-6446cc5af664");

    [Fact]
    public async Task FavoritesRequireAuthentication()
    {
        using var application = CreateApplication(new TestFavoriteService());
        using var client = application.CreateClient();

        var response = await client.GetAsync("/api/v1/account/favorites");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUserCanListAddAndRemoveFavorites()
    {
        var favorites = new TestFavoriteService();
        using var application = CreateApplication(favorites);
        using var client = application.CreateClient();
        await Authenticate(client);

        var addResponse = await client.PostAsync(
            $"/api/v1/account/favorites/{ProductId}",
            null
        );
        var listResponse = await client.GetAsync(
            "/api/v1/account/favorites?page=1&pageSize=10"
        );
        var list = await listResponse.Content.ReadFromJsonAsync<PagedFavoriteResult>();
        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/account/favorites/{ProductId}"
        );

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(list);
        Assert.Single(list.Items);
        Assert.Equal(UserId, favorites.LastUserId);
        Assert.Equal(ProductId, favorites.LastProductId);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.True(favorites.RemoveCalled);
    }

    [Fact]
    public async Task FavoritesRejectInvalidPagination()
    {
        using var application = CreateApplication(new TestFavoriteService());
        using var client = application.CreateClient();
        await Authenticate(client);

        var response = await client.GetAsync(
            "/api/v1/account/favorites?page=0&pageSize=101"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task Authenticate(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new
            {
                Email = "umay@sofistike.com",
                Password = "Umay123!",
                RememberMe = false,
            }
        );
        var login = await response.Content.ReadFromJsonAsync<LoginPayload>();
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    private static WebApplicationFactory<Program> CreateApplication(
        IFavoriteService favoriteService
    )
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
                    services.RemoveAll<IFavoriteService>();
                    services.AddSingleton(favoriteService);
                });
            }
        );
    }

    private sealed record LoginPayload(string AccessToken);

    private sealed class TestCredentialValidator : ICredentialValidator
    {
        public Task<AuthenticatedUser?> ValidateAsync(
            LoginCommand command,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<AuthenticatedUser?>(new AuthenticatedUser(
                UserId,
                "umay@sofistike.com",
                "Umay",
                "Customer"
            ));
        }
    }

    private sealed class TestFavoriteService : IFavoriteService
    {
        private readonly FavoriteItem _favorite = new(
            DateTimeOffset.UtcNow,
            new ProductCard(
                ProductId,
                "XTRA-SLEEP-001",
                "+XTRA One Konfor Yastığı",
                "xtra-one-konfor-yastigi",
                "Ayarlanabilir dolgu ile kişiselleştirilen uyku konforu.",
                true,
                true,
                null,
                new ProductPriceSummary(999m, null, 999m, "TRY", null),
                new StockSummary(12, "InStock"),
                []
            )
        );

        public Guid LastUserId { get; private set; }
        public Guid LastProductId { get; private set; }
        public bool RemoveCalled { get; private set; }

        public Task<PagedFavoriteResult> GetAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default
        )
        {
            LastUserId = userId;
            return Task.FromResult(
                new PagedFavoriteResult([_favorite], page, pageSize, 1, 1)
            );
        }

        public Task<AddFavoriteResult> AddAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default
        )
        {
            LastUserId = userId;
            LastProductId = productId;
            return Task.FromResult(
                new AddFavoriteResult(AddFavoriteStatus.Added, _favorite)
            );
        }

        public Task RemoveAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default
        )
        {
            LastUserId = userId;
            LastProductId = productId;
            RemoveCalled = true;
            return Task.CompletedTask;
        }
    }
}
