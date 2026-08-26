using System.Globalization;
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

namespace Sofistike.IntegrationTests;

public sealed class AdminProductEndpointTests
{
    private static readonly Guid ProductId =
        Guid.Parse("3310ead5-3459-43a7-982f-6446cc5af664");

    [Fact]
    public async Task ProductMutationsRequireAdministratorRole()
    {
        var management = new TestProductManagementService();
        using var application = CreateApplication(management, "Customer");
        using var client = application.CreateClient();

        var anonymousResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/products/{ProductId}",
            ValidProductRequest()
        );
        await Authenticate(client);
        var customerResponse = await client.DeleteAsync(
            $"/api/v1/admin/products/{ProductId}"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, customerResponse.StatusCode);
        Assert.Null(management.LastUpdateCommand);
        Assert.Null(management.LastArchivedProductId);
    }

    [Fact]
    public async Task AdministratorCanUpdateAndArchiveProduct()
    {
        var management = new TestProductManagementService();
        using var application = CreateApplication(management, "Admin");
        using var client = application.CreateClient();
        await Authenticate(client);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/products/{ProductId}",
            ValidProductRequest()
        );
        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/admin/products/{ProductId}"
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(ProductId, management.LastUpdatedProductId);
        Assert.Equal(749.90m, management.LastUpdateCommand?.Price);
        Assert.Equal(2, management.LastUpdateCommand?.CategoryIds.Count);
        Assert.Equal(ProductId, management.LastArchivedProductId);
    }

    [Fact]
    public async Task ProductPriceValidationIsCultureIndependent()
    {
        var management = new TestProductManagementService();
        using var application = CreateApplication(management, "Admin");
        using var client = application.CreateClient();
        await Authenticate(client);
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var response = await client.PostAsJsonAsync(
                "/api/v1/admin/products",
                ValidProductRequest()
            );

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static object ValidProductRequest() => new
    {
        ProductCode = "SLEEP-EDIT-001",
        Name = "Düzenlenen Konfor Yastığı",
        Slug = "duzenlenen-konfor-yastigi",
        ShortDescription = "Güncel kısa ürün açıklaması.",
        Description = "Güncel ve ayrıntılı ürün açıklaması.",
        CategoryIds = new[]
        {
            Guid.Parse("a8dbe6dd-d2d3-4e8a-abab-fe73a4ed51e0"),
            Guid.Parse("b521da36-abbe-408b-b191-2cb24c2bb7b5"),
        },
        Price = 749.90m,
        StockQuantity = 24,
        ImageUrl = "/images/hero-sleep.png",
        IsPopular = true,
        IsXtra = false,
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
        IProductManagementService productManagementService,
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
                services.RemoveAll<IProductManagementService>();
                services.AddSingleton(productManagementService);
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

    private sealed class TestProductManagementService
        : IProductManagementService
    {
        public Guid? LastUpdatedProductId { get; private set; }
        public UpdateProductCommand? LastUpdateCommand { get; private set; }
        public Guid? LastArchivedProductId { get; private set; }

        public Task<CreateProductResult> CreateAsync(
            CreateProductCommand command,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new CreateProductResult(
            CreateProductStatus.Created,
            Product()
        ));

        public Task<UpdateProductResult> UpdateAsync(
            Guid productId,
            UpdateProductCommand command,
            CancellationToken cancellationToken = default
        )
        {
            LastUpdatedProductId = productId;
            LastUpdateCommand = command;
            return Task.FromResult(new UpdateProductResult(
                UpdateProductStatus.Updated,
                Product()
            ));
        }

        public Task<ArchiveProductStatus> ArchiveAsync(
            Guid productId,
            CancellationToken cancellationToken = default
        )
        {
            LastArchivedProductId = productId;
            return Task.FromResult(ArchiveProductStatus.Archived);
        }

        private static ProductDetails Product() => new(
            ProductId,
            "SLEEP-EDIT-001",
            "Düzenlenen Konfor Yastığı",
            "duzenlenen-konfor-yastigi",
            "Güncel kısa ürün açıklaması.",
            "Güncel ve ayrıntılı ürün açıklaması.",
            true,
            false,
            DateTimeOffset.UtcNow,
            [],
            [],
            []
        );
    }
}
