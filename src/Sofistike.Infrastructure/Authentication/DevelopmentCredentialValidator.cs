using System.Security.Cryptography;
using System.Text;
using Sofistike.Application.Authentication;

namespace Sofistike.Infrastructure.Authentication;

public sealed record DevelopmentUserOptions(
    Guid Id,
    string Email,
    string FirstName,
    string Role,
    string PasswordSalt,
    string PasswordHash,
    int PasswordIterations
);

public sealed class DevelopmentCredentialValidator(
    DevelopmentUserOptions options
) : ICredentialValidator
{
    public AuthenticatedUser? Validate(LoginCommand command)
    {
        if (!string.Equals(
                command.Email.Trim(),
                options.Email,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return null;
        }

        var salt = Convert.FromBase64String(options.PasswordSalt);
        var expectedHash = Convert.FromBase64String(options.PasswordHash);
        var suppliedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(command.Password),
            salt,
            options.PasswordIterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length
        );

        return CryptographicOperations.FixedTimeEquals(
            suppliedHash,
            expectedHash
        )
            ? new AuthenticatedUser(
                options.Id,
                options.Email,
                options.FirstName,
                options.Role
            )
            : null;
    }
}
