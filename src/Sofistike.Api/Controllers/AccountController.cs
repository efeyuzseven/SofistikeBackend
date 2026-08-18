using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Users;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/account")]
public sealed class AccountController(
    IUserProfileService userProfileService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    [HttpGet("profile")]
    [ProducesResponseType<UserProfile>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfile>> GetProfile(
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem();
        }

        var profile = await userProfileService.GetAsync(
            ticket.UserId,
            cancellationToken
        );

        return profile is null ? UserNotFoundProblem() : Ok(profile);
    }

    [HttpPut("profile")]
    [ProducesResponseType<UserProfile>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfile>> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem();
        }

        var profile = await userProfileService.UpdateAsync(
            new UpdateUserProfileCommand(
                ticket.UserId,
                request.FirstName,
                request.LastName,
                request.PhoneNumber
            ),
            cancellationToken
        );

        return profile is null ? UserNotFoundProblem() : Ok(profile);
    }

    private SessionTicket? ReadSessionTicket()
    {
        const string bearerPrefix = "Bearer ";
        var authorization = Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith(
            bearerPrefix,
            StringComparison.OrdinalIgnoreCase
        )
            ? authorization[bearerPrefix.Length..].Trim()
            : null;

        return string.IsNullOrWhiteSpace(token)
            ? null
            : sessionTicketService.Read(token);
    }

    private ActionResult<UserProfile> UnauthorizedProblem()
    {
        return Unauthorized(
            CreateProblem(
                StatusCodes.Status401Unauthorized,
                "Oturum bulunamadı",
                "Profil bilgilerinize erişmek için yeniden giriş yapın."
            )
        );
    }

    private ActionResult<UserProfile> UserNotFoundProblem()
    {
        return NotFound(
            CreateProblem(
                StatusCodes.Status404NotFound,
                "Kullanıcı bulunamadı",
                "Profil kaydı bulunamadı veya kullanıcı aktif değil."
            )
        );
    }

    private ProblemDetails CreateProblem(int status, string title, string detail)
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

public sealed class UpdateProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }

    [StringLength(30)]
    public string? PhoneNumber { get; init; }
}
