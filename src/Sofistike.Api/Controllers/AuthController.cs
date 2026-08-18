using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Authentication;
using Sofistike.Application.Users;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    ICredentialValidator credentialValidator,
    IUserRegistrationService userRegistrationService,
    IUserProfileService userProfileService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    private static readonly TimeSpan StandardSessionLifetime =
        TimeSpan.FromHours(8);
    private static readonly TimeSpan RememberedSessionLifetime =
        TimeSpan.FromDays(30);

    [HttpPost("register")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            ModelState.AddModelError(
                nameof(request.FirstName),
                "Ad alanı boş bırakılamaz."
            );
            return ValidationProblem(ModelState);
        }

        if (!MeetsPasswordPolicy(request.Password))
        {
            ModelState.AddModelError(
                nameof(request.Password),
                "Şifre büyük harf, küçük harf, rakam ve özel karakter içermelidir."
            );
            return ValidationProblem(ModelState);
        }

        var result = await userRegistrationService.RegisterAsync(
            new RegisterUserCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName
            ),
            cancellationToken
        );

        if (result.Status == UserRegistrationStatus.EmailAlreadyExists)
        {
            return Conflict(
                CreateProblem(
                    StatusCodes.Status409Conflict,
                    "E-posta zaten kayıtlı",
                    "Bu e-posta adresiyle daha önce bir hesap oluşturulmuş."
                )
            );
        }

        var user = result.User
            ?? throw new InvalidOperationException(
                "Created registration result does not contain a user."
            );
        var token = sessionTicketService.Create(user, StandardSessionLifetime);

        return StatusCode(
            StatusCodes.Status201Created,
            new LoginResponse(
                new UserResponse(
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.Role
                ),
                token,
                (int)StandardSessionLifetime.TotalSeconds
            )
        );
    }

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var user = await credentialValidator.ValidateAsync(
            new LoginCommand(
                request.Email,
                request.Password,
                request.RememberMe
            ),
            cancellationToken
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
    public async Task<ActionResult<UserResponse>> Me(
        CancellationToken cancellationToken
    )
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

        var profile = await userProfileService.GetAsync(
            ticket.UserId,
            cancellationToken
        );

        if (profile is null)
        {
            return Unauthorized(
                CreateProblem(
                    StatusCodes.Status401Unauthorized,
                    "Kullanıcı bulunamadı",
                    "Oturuma ait kullanıcı bulunamadı veya aktif değil."
                )
            );
        }

        return Ok(
            new UserResponse(
                profile.Id,
                profile.Email,
                profile.FirstName,
                profile.Role
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

    private static bool MeetsPasswordPolicy(string password)
    {
        return password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(character => !char.IsLetterOrDigit(character));
    }
}

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public required string Email { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public required string Password { get; init; }
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
