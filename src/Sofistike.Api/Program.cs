using Microsoft.AspNetCore.DataProtection;
using Sofistike.Api.Authentication;
using Sofistike.Application.Authentication;
using Sofistike.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys"))
    );

var developmentUser = builder.Configuration
    .GetRequiredSection("Authentication:DevelopmentUser")
    .Get<DevelopmentUserOptions>()
    ?? throw new InvalidOperationException(
        "Authentication development user configuration is missing."
    );

builder.Services.AddSingleton<ICredentialValidator>(
    new DevelopmentCredentialValidator(developmentUser)
);
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

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(frontendCorsPolicy);
app.MapControllers();

app.Run();

public partial class Program;
