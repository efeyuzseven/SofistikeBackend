namespace Sofistike.Application.Authentication;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string? LastName
);

public enum UserRegistrationStatus
{
    Created,
    EmailAlreadyExists,
}

public sealed record UserRegistrationResult(
    UserRegistrationStatus Status,
    AuthenticatedUser? User
);

public interface IUserRegistrationService
{
    Task<UserRegistrationResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default
    );
}
