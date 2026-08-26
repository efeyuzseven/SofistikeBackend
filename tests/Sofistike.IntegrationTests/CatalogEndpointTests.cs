using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sofistike.Application.Catalog;
using Sofistike.Domain.Catalog;

namespace Sofistike.IntegrationTests;

public sealed class CatalogEndpointTests
{
    [Fact]
    public async Task ProductsMapsFiltersAndPagination()
    {
        var catalog = new TestProductCatalogService();
        using var application = CreateApplication(catalog);
        using var client = application.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/products?category=uyku&isXtra=true&inStock=true&minPrice=100&maxPrice=1200&sort=price-desc&page=2&pageSize=5"
        );
        var payload = await response.Content.ReadFromJsonAsync<PagedProductResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(catalog.LastQuery);
        Assert.Equal("uyku", catalog.LastQuery.Category);
        Assert.True(catalog.LastQuery.IsXtra);
        Assert.True(catalog.LastQuery.InStock);
        Assert.Equal(100m, catalog.LastQuery.MinimumPrice);
        Assert.Equal(1200m, catalog.LastQuery.MaximumPrice);
        Assert.Equal(ProductSort.PriceDescending, catalog.LastQuery.Sort);
        Assert.Equal(2, catalog.LastQuery.Page);
        Assert.Equal(5, catalog.LastQuery.PageSize);
    }

    [Fact]
    public async Task ProductsRejectsInvalidQuery()
    {
        using var application = CreateApplication(new TestProductCatalogService());
        using var client = application.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/products?minPrice=500&maxPrice=100&sort=unknown&page=0&pageSize=101"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProductDetailReturnsNotFound()
    {
        using var application = CreateApplication(new TestProductCatalogService());
        using var client = application.CreateClient();

        var response = await client.GetAsync("/api/v1/products/bilinmeyen-urun");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CategoriesReturnsActiveCatalogCategories()
    {
        var catalog = new TestProductCatalogService
        {
            Categories =
            [
                new CategorySummary(
                    Guid.Parse("a8dbe6dd-d2d3-4e8a-abab-fe73a4ed51e0"),
                    "Uyku",
                    "uyku",
                    null,
                    null,
                    CategoryMenuGroup.Category,
                    1
                ),
            ],
        };
        using var application = CreateApplication(catalog);
        using var client = application.CreateClient();

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var categories = await client.GetFromJsonAsync<List<CategorySummary>>(
            "/api/v1/categories",
            jsonOptions
        );

        Assert.Single(categories ?? []);
        Assert.Equal("uyku", categories![0].Slug);
    }

    private static WebApplicationFactory<Program> CreateApplication(
        IProductCatalogService catalogService
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
                    services.RemoveAll<IProductCatalogService>();
                    services.AddSingleton(catalogService);
                });
            }
        );
    }

    private sealed class TestProductCatalogService : IProductCatalogService
    {
        public ProductListQuery? LastQuery { get; private set; }
        public IReadOnlyList<CategorySummary> Categories { get; init; } = [];

        public Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Categories);
        }

        public Task<PagedProductResult> GetProductsAsync(
            ProductListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            LastQuery = query;
            return Task.FromResult(
                new PagedProductResult([], query.Page, query.PageSize, 0, 0)
            );
        }

        public Task<ProductDetails?> GetProductBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<ProductDetails?>(null);
        }
    }
}
