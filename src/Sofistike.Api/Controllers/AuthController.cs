using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Authentication;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    ICredentialValidator credentialValidator,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    private static readonly TimeSpan StandardSessionLifetime =
        TimeSpan.FromHours(8);
    private static readonly TimeSpan RememberedSessionLifetime =
        TimeSpan.FromDays(30);

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var user = credentialValidator.Validate(
            new LoginCommand(
                request.Email,
                request.Password,
                request.RememberMe
            )
        );

        if (user is null)
        {
            return Unauthorized(
                CreateProblem(
                    StatusCodes.Status401Unauthorized,
                    "Giriş başarısız",
                    "E-posta adresi veya şifre hatalı."
                )
            );
        }

        var lifetime = request.RememberMe
            ? RememberedSessionLifetime
            : StandardSessionLifetime;
        var token = sessionTicketService.Create(user, lifetime);

        return Ok(
            new LoginResponse(
                new UserResponse(
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.Role
                ),
                token,
                (int)lifetime.TotalSeconds
            )
        );
    }

    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserResponse> Me()
    {
        var token = ReadBearerToken();
        var ticket = token is null ? null : sessionTicketService.Read(token);

        if (ticket is null)
        {
            return Unauthorized(
                CreateProblem(
                    StatusCodes.Status401Unauthorized,
                    "Oturum bulunamadı",
                    "Oturumunuz geçersiz veya süresi dolmuş."
                )
            );
        }

        return Ok(
            new UserResponse(
                ticket.UserId,
                ticket.Email,
                ticket.FirstName,
                ticket.Role
            )
        );
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        return NoContent();
    }

    private string? ReadBearerToken()
    {
        const string bearerPrefix = "Bearer ";
        var authorization = Request.Headers.Authorization.ToString();

        return authorization.StartsWith(
            bearerPrefix,
            StringComparison.OrdinalIgnoreCase
        )
            ? authorization[bearerPrefix.Length..].Trim()
            : null;
    }

    private ProblemDetails CreateProblem(
        int status,
        string title,
        string detail
    )
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path,
        };
    }
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(8)]
    public required string Password { get; init; }

    public bool RememberMe { get; init; }
}

public sealed record LoginResponse(
    UserResponse User,
    string AccessToken,
    int ExpiresIn
);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string Role
);
