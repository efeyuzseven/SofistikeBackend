using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Users;
using Sofistike.Domain.Users;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Users;

public sealed class UserProfileService(SofistikeDbContext dbContext)
    : IUserProfileService
{
    public async Task<UserProfile?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == userId && candidate.IsActive,
                cancellationToken
            );

        return user is null ? null : Map(user);
    }

    public async Task<UserProfile?> UpdateAsync(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == command.UserId && candidate.IsActive,
            cancellationToken
        );

        if (user is null)
        {
            return null;
        }

        user.FirstName = command.FirstName.Trim();
        user.LastName = NormalizeOptional(command.LastName);
        user.PhoneNumber = NormalizeOptional(command.PhoneNumber);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    private static UserProfile Map(UserAccount user)
    {
        return new UserProfile(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.Role
        );
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
