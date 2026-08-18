using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Authentication;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Authentication;

public sealed class DatabaseCredentialValidator(SofistikeDbContext dbContext)
    : ICredentialValidator
{
    public async Task<AuthenticatedUser?> ValidateAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.NormalizedEmail == normalizedEmail
                    && candidate.IsActive,
                cancellationToken
            );

        if (user is null)
        {
            return null;
        }

        return PasswordHashing.Verify(
            command.Password,
            user.PasswordSalt,
            user.PasswordHash,
            user.PasswordIterations
        )
            ? new AuthenticatedUser(
                user.Id,
                user.Email,
                user.FirstName,
                user.Role
            )
            : null;
    }
}
