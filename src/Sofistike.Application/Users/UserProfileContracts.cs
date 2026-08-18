namespace Sofistike.Application.Users;

public sealed record UserProfile(
    Guid Id,
    string Email,
    string FirstName,
    string? LastName,
    string? PhoneNumber,
    string Role
);

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string FirstName,
    string? LastName,
    string? PhoneNumber
);

public interface IUserProfileService
{
    Task<UserProfile?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<UserProfile?> UpdateAsync(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken = default
    );
}
