using System.Security.Cryptography;
using System.Text;

namespace Sofistike.Infrastructure.Authentication;

internal sealed record PasswordHashResult(
    string Salt,
    string Hash,
    int Iterations
);

internal static class PasswordHashing
{
    private const int DefaultIterations = 120_000;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    public static PasswordHashResult Create(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            HashLength
        );

        return new PasswordHashResult(
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            DefaultIterations
        );
    }

    public static bool Verify(
        string password,
        string encodedSalt,
        string encodedHash,
        int iterations
    )
    {
        var salt = Convert.FromBase64String(encodedSalt);
        var expectedHash = Convert.FromBase64String(encodedHash);
        var suppliedHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length
        );

        return CryptographicOperations.FixedTimeEquals(
            suppliedHash,
            expectedHash
        );
    }
}
