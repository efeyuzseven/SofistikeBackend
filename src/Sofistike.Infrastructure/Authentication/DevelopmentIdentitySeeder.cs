using Microsoft.EntityFrameworkCore;
using Sofistike.Domain.Users;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Authentication;

public static class DevelopmentIdentitySeeder
{
    public static async Task SeedAsync(
        SofistikeDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        await IdentitySeeder.EnsureUserAsync(
            dbContext,
            "admin@sofistike.com",
            "Admin123!",
            "Sofistike",
            "Admin",
            cancellationToken
        );
        await IdentitySeeder.EnsureUserAsync(
            dbContext,
            "umay@sofistike.com",
            "Umay123!",
            "Umay",
            "Customer",
            cancellationToken
        );
    }
}

public static class DeploymentIdentitySeeder
{
    public static Task SeedAdminAsync(
        SofistikeDbContext dbContext,
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return IdentitySeeder.EnsureUserAsync(
            dbContext,
            email.Trim(),
            password,
            "Sofistike",
            "Admin",
            cancellationToken
        );
    }
}

internal static class IdentitySeeder
{
    public static async Task EnsureUserAsync(
        SofistikeDbContext dbContext,
        string email,
        string password,
        string firstName,
        string role,
        CancellationToken cancellationToken
    )
    {
        var normalizedEmail = email.ToUpperInvariant();
        var existing = await dbContext.Users.SingleOrDefaultAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken
        );
        if (existing is not null)
        {
            if (existing.Role != role || !existing.IsActive)
            {
                existing.Role = role;
                existing.IsActive = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var passwordHash = PasswordHashing.Create(password);
        dbContext.Users.Add(new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            FirstName = firstName,
            Role = role,
            PasswordSalt = passwordHash.Salt,
            PasswordHash = passwordHash.Hash,
            PasswordIterations = passwordHash.Iterations,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
