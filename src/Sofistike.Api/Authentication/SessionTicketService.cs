using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Sofistike.Application.Authentication;

namespace Sofistike.Api.Authentication;

public sealed record SessionTicket(
    Guid UserId,
    string Email,
    string FirstName,
    string Role,
    DateTimeOffset ExpiresAt
);

public interface ISessionTicketService
{
    string Create(AuthenticatedUser user, TimeSpan lifetime);

    SessionTicket? Read(string token);
}

public sealed class SessionTicketService(
    IDataProtectionProvider dataProtectionProvider
) : ISessionTicketService
{
    private static readonly JsonSerializerOptions JsonOptions = new(
        JsonSerializerDefaults.Web
    );

    private readonly IDataProtector _protector = dataProtectionProvider
        .CreateProtector("Sofistike.Authentication.SessionTicket.v1");

    public string Create(AuthenticatedUser user, TimeSpan lifetime)
    {
        var ticket = new SessionTicket(
            user.Id,
            user.Email,
            user.FirstName,
            user.Role,
            DateTimeOffset.UtcNow.Add(lifetime)
        );

        return _protector.Protect(JsonSerializer.Serialize(ticket, JsonOptions));
    }

    public SessionTicket? Read(string token)
    {
        try
        {
            var payload = _protector.Unprotect(token);
            var ticket = JsonSerializer.Deserialize<SessionTicket>(
                payload,
                JsonOptions
            );

            return ticket is not null && ticket.ExpiresAt > DateTimeOffset.UtcNow
                ? ticket
                : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
