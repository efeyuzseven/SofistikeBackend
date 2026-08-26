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
using Sofistike.Domain.Catalog;

namespace Sofistike.IntegrationTests;

public sealed class AdminCategoryEndpointTests
{
    private static readonly Guid CategoryId =
        Guid.Parse("fc6819d5-eec1-4886-bfc3-f4f0fb1d44bf");

    [Fact]
    public async Task CategoryManagementRequiresAdministratorRole()
    {
        var service = new TestCategoryManagementService();
        using var application = CreateApplication(service, "Customer");
        using var client = application.CreateClient();

        var anonymousResponse = await client.GetAsync("/api/v1/admin/categories");
        await Authenticate(client);
        var customerResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/categories",
            ValidCategoryRequest()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, customerResponse.StatusCode);
        Assert.Null(service.LastCommand);
    }

    [Fact]
    public async Task AdministratorCanCreateUpdateAndDeleteCategory()
    {
        var service = new TestCategoryManagementService();
        using var application = CreateApplication(service, "Admin");
        using var client = application.CreateClient();
        await Authenticate(client);

        var listResponse = await client.GetAsync("/api/v1/admin/categories");
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/categories",
            ValidCategoryRequest()
        );
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/admin/categories/{CategoryId}",
            ValidCategoryRequest()
        );
        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/admin/categories/{CategoryId}"
        );

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(CategoryMenuGroup.Room, service.LastCommand?.MenuGroup);
        Assert.Equal(CategoryId, service.LastUpdatedId);
        Assert.Equal(CategoryId, service.LastDeletedId);
    }

    [Fact]
    public async Task CategoryRejectsUnknownMenuGroup()
    {
        var service = new TestCategoryManagementService();
        using var application = CreateApplication(service, "Admin");
        using var client = application.CreateClient();
        await Authenticate(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/categories",
            new
            {
                Name = "Bilinmeyen grup",
                Slug = "bilinmeyen-grup",
                MenuGroup = "Unknown",
                DisplayOrder = 1,
                IsActive = true,
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(service.LastCommand);
    }

    private static object ValidCategoryRequest() => new
    {
        Name = "Çalışma Odası",
        Slug = "calisma-odasi",
        Description = "Çalışma odasına uygun ürünler.",
        MenuGroup = "Room",
        DisplayOrder = 3,
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
        ICategoryManagementService categoryManagementService,
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
                services.RemoveAll<ICategoryManagementService>();
                services.AddSingleton(categoryManagementService);
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

    private sealed class TestCategoryManagementService
        : ICategoryManagementService
    {
        public SaveCategoryCommand? LastCommand { get; private set; }
        public Guid? LastUpdatedId { get; private set; }
        public Guid? LastDeletedId { get; private set; }

        public Task<IReadOnlyList<ManagedCategoryDetails>> GetAllAsync(
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<ManagedCategoryDetails>>([Category()]);

        public Task<SaveCategoryResult> CreateAsync(
            SaveCategoryCommand command,
            CancellationToken cancellationToken = default
        )
        {
            LastCommand = command;
            return Task.FromResult(new SaveCategoryResult(
                SaveCategoryStatus.Saved,
                Category()
            ));
        }

        public Task<SaveCategoryResult> UpdateAsync(
            Guid categoryId,
            SaveCategoryCommand command,
            CancellationToken cancellationToken = default
        )
        {
            LastUpdatedId = categoryId;
            LastCommand = command;
            return Task.FromResult(new SaveCategoryResult(
                SaveCategoryStatus.Saved,
                Category()
            ));
        }

        public Task<DeleteCategoryStatus> DeleteAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default
        )
        {
            LastDeletedId = categoryId;
            return Task.FromResult(DeleteCategoryStatus.Deleted);
        }

        private static ManagedCategoryDetails Category() => new(
            CategoryId,
            "Çalışma Odası",
            "calisma-odasi",
            null,
            CategoryMenuGroup.Room,
            3,
            true,
            0,
            DateTimeOffset.UtcNow
        );
    }
}
