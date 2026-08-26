using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Sofistike.Api.Authentication;
using Sofistike.Application.Authentication;
using Sofistike.Application.Catalog;
using Sofistike.Application.Content;
using Sofistike.Application.Favorites;
using Sofistike.Application.Reviews;
using Sofistike.Application.Sales;
using Sofistike.Application.Users;
using Sofistike.Infrastructure.Authentication;
using Sofistike.Infrastructure.Catalog;
using Sofistike.Infrastructure.Content;
using Sofistike.Infrastructure.Favorites;
using Sofistike.Infrastructure.Persistence;
using Sofistike.Infrastructure.Reviews;
using Sofistike.Infrastructure.Sales;
using Sofistike.Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);
var isBootstrapCommand = args.Contains(
    "--bootstrap",
    StringComparer.OrdinalIgnoreCase
);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var databaseConnectionString = builder.Configuration.GetConnectionString(
    "SofistikeDatabase"
) ?? throw new InvalidOperationException(
    "Connection string 'SofistikeDatabase' is missing."
);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<SofistikeDbContext>(options =>
    options.UseSqlServer(databaseConnectionString)
);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys"))
    );

builder.Services.AddScoped<ICredentialValidator, DatabaseCredentialValidator>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<IProductManagementService, ProductManagementService>();
builder.Services.AddScoped<IHomeBannerService, HomeBannerService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<ISessionTicketService, SessionTicketService>();

const string frontendCorsPolicy = "Frontend";
var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        frontendCorsPolicy,
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (isBootstrapCommand)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SofistikeDbContext>();
    var adminPassword = app.Configuration["DeploymentBootstrap:AdminPassword"];

    if (string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Deployment bootstrap admin password is missing."
        );
    }

    var adminEmail = app.Configuration["DeploymentBootstrap:AdminEmail"]
        ?? "admin@sofistike.com";

    await dbContext.Database.MigrateAsync();
    await DeploymentIdentitySeeder.SeedAdminAsync(
        dbContext,
        adminEmail,
        adminPassword
    );
    await DevelopmentCatalogSeeder.SeedAsync(dbContext);
    await DevelopmentContentSeeder.SeedAsync(dbContext);
    return;
}

if (
    app.Environment.IsDevelopment()
    && app.Configuration.GetValue<bool>("Catalog:SeedDevelopmentData")
)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SofistikeDbContext>();
    await dbContext.Database.MigrateAsync();
    await DevelopmentIdentitySeeder.SeedAsync(dbContext);
    await DevelopmentCatalogSeeder.SeedAsync(dbContext);
    await DevelopmentContentSeeder.SeedAsync(dbContext);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);
app.MapControllers();

app.Run();

public partial class Program;
