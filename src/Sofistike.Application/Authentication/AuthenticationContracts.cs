namespace Sofistike.Application.Authentication;

public sealed record LoginCommand(
    string Email,
    string Password,
    bool RememberMe
);

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string FirstName,
    string Role
);

public interface ICredentialValidator
{
    AuthenticatedUser? Validate(LoginCommand command);
}
