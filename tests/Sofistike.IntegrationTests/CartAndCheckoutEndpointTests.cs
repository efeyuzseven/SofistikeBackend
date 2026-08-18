using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sofistike.Application.Authentication;
using Sofistike.Application.Sales;

namespace Sofistike.IntegrationTests;

public sealed class CartAndCheckoutEndpointTests
{
    private static readonly Guid UserId =
        Guid.Parse("d8fbd714-b22f-4a7f-b576-c6a2183f6e80");
    private static readonly Guid ProductId =
        Guid.Parse("3310ead5-3459-43a7-982f-6446cc5af664");

    [Fact]
    public async Task CartAndOrdersRequireAuthentication()
    {
        using var application = CreateApplication(
            new TestCartService(),
            new TestOrderService()
        );
        using var client = application.CreateClient();

        var cartResponse = await client.GetAsync("/api/v1/account/cart");
        var orderResponse = await client.PostAsJsonAsync(
            "/api/v1/account/orders",
            ValidOrderRequest()
        );

        Assert.Equal(HttpStatusCode.Unauthorized, cartResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, orderResponse.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUserCanMutateCartAndCreateOrder()
    {
        var cart = new TestCartService();
        var orders = new TestOrderService();
        using var application = CreateApplication(cart, orders);
        using var client = application.CreateClient();
        await Authenticate(client);

        var addResponse = await client.PostAsJsonAsync(
            "/api/v1/account/cart/items",
            new { ProductId, Quantity = 2 }
        );
        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/account/cart/items/{ProductId}",
            new { Quantity = 3 }
        );
        var orderResponse = await client.PostAsJsonAsync(
            "/api/v1/account/orders",
            ValidOrderRequest()
        );
        var order = await orderResponse.Content.ReadFromJsonAsync<CreatedOrder>();

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(3, cart.LastQuantity);
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        Assert.NotNull(order);
        Assert.Equal("Pending", order.PaymentStatus);
        Assert.Equal(UserId, orders.LastUserId);
        Assert.Equal("Umay", orders.LastCommand?.FirstName);
        Assert.Equal("standard", orders.LastCommand?.DeliveryMethod);
    }

    private static object ValidOrderRequest() => new
    {
        ContactEmail = "umay@sofistike.com",
        FirstName = "Umay",
        LastName = "Test",
        PhoneNumber = "05555555555",
        City = "İstanbul",
        District = "Kadıköy",
        AddressLine = "Test Mahallesi No: 1",
        PostalCode = "34710",
        AddressTitle = "Ev",
        DeliveryMethod = "standard",
    };

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
        ICartService cartService,
        IOrderService orderService
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
                    services.RemoveAll<ICartService>();
                    services.AddSingleton(cartService);
                    services.RemoveAll<IOrderService>();
                    services.AddSingleton(orderService);
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
        ) => Task.FromResult<AuthenticatedUser?>(new AuthenticatedUser(
            UserId,
            "umay@sofistike.com",
            "Umay",
            "Customer"
        ));
    }

    private sealed class TestCartService : ICartService
    {
        public int LastQuantity { get; private set; }

        public Task<CartSummary> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(EmptyCart());

        public Task<CartMutationResult> AddAsync(
            Guid userId,
            Guid productId,
            int quantity,
            CancellationToken cancellationToken = default
        )
        {
            LastQuantity = quantity;
            return Task.FromResult(Success());
        }

        public Task<CartMutationResult> UpdateAsync(
            Guid userId,
            Guid productId,
            int quantity,
            CancellationToken cancellationToken = default
        )
        {
            LastQuantity = quantity;
            return Task.FromResult(Success());
        }

        public Task<CartSummary> RemoveAsync(
            Guid userId,
            Guid productId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(EmptyCart());

        private static CartMutationResult Success() =>
            new(CartMutationStatus.Success, EmptyCart());

        private static CartSummary EmptyCart() =>
            new([], 0, 0m, "TRY", DateTimeOffset.UtcNow);
    }

    private sealed class TestOrderService : IOrderService
    {
        public Guid LastUserId { get; private set; }
        public CreateOrderCommand? LastCommand { get; private set; }

        public Task<CreateOrderResult> CreateAsync(
            Guid userId,
            CreateOrderCommand command,
            CancellationToken cancellationToken = default
        )
        {
            LastUserId = userId;
            LastCommand = command;
            return Task.FromResult(new CreateOrderResult(
                CreateOrderStatus.Created,
                new CreatedOrder(
                    Guid.NewGuid(),
                    "SFX-TEST-001",
                    "AwaitingPayment",
                    "Pending",
                    999m,
                    0m,
                    999m,
                    "TRY",
                    DateTimeOffset.UtcNow
                )
            ));
        }
    }
}
