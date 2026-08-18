using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Authentication;
using Sofistike.Domain.Users;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Authentication;

public sealed class UserRegistrationService(SofistikeDbContext dbContext)
    : IUserRegistrationService
{
    public async Task<UserRegistrationResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var email = command.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        if (await EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return EmailAlreadyExists();
        }

        var password = PasswordHashing.Create(command.Password);
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            FirstName = command.FirstName.Trim(),
            LastName = NormalizeOptional(command.LastName),
            PhoneNumber = null,
            Role = "Customer",
            PasswordSalt = password.Salt,
            PasswordHash = password.Hash,
            PasswordIterations = password.Iterations,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(user).State = EntityState.Detached;

            if (await EmailExistsAsync(normalizedEmail, cancellationToken))
            {
                return EmailAlreadyExists();
            }

            throw;
        }

        return new UserRegistrationResult(
            UserRegistrationStatus.Created,
            new AuthenticatedUser(
                user.Id,
                user.Email,
                user.FirstName,
                user.Role
            )
        );
    }

    private Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken
    )
    {
        return dbContext.Users.AnyAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail,
            cancellationToken
        );
    }

    private static UserRegistrationResult EmailAlreadyExists()
    {
        return new UserRegistrationResult(
            UserRegistrationStatus.EmailAlreadyExists,
            null
        );
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
