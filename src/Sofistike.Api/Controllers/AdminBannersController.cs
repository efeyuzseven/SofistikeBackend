using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Content;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/admin/banners")]
public sealed class AdminBannersController(
    IHomeBannerService homeBannerService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<HomeBannerDetails>>(
        StatusCodes.Status200OK
    )]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBanners(
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        return Ok(await homeBannerService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<HomeBannerDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBanner(
        SaveHomeBannerRequest request,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var addressErrors = ValidateAddresses(request);
        if (addressErrors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(addressErrors));
        }

        var banner = await homeBannerService.CreateAsync(
            request.ToCommand(),
            cancellationToken
        );
        return StatusCode(StatusCodes.Status201Created, banner);
    }

    [HttpPut("{bannerId:guid}")]
    [ProducesResponseType<HomeBannerDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBanner(
        Guid bannerId,
        SaveHomeBannerRequest request,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var addressErrors = ValidateAddresses(request);
        if (addressErrors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(addressErrors));
        }

        var banner = await homeBannerService.UpdateAsync(
            bannerId,
            request.ToCommand(),
            cancellationToken
        );
        return banner is null
            ? ProblemResponse(
                StatusCodes.Status404NotFound,
                "Banner bulunamadı",
                "Güncellenmek istenen banner bulunamadı."
            )
            : Ok(banner);
    }

    [HttpDelete("{bannerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBanner(
        Guid bannerId,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        return await homeBannerService.DeleteAsync(bannerId, cancellationToken)
            ? NoContent()
            : ProblemResponse(
                StatusCodes.Status404NotFound,
                "Banner bulunamadı",
                "Silinmek istenen banner bulunamadı."
            );
    }

    private static Dictionary<string, string[]> ValidateAddresses(
        SaveHomeBannerRequest request
    )
    {
        var errors = new Dictionary<string, string[]>();
        if (!IsRelativeOrHttpsAddress(request.ImageUrl, allowAnchor: false))
        {
            errors["imageUrl"] =
            ["Görsel adresi / ile başlayan yerel bir yol veya HTTPS adresi olmalıdır."];
        }

        if (
            !string.IsNullOrWhiteSpace(request.LinkUrl)
            && !IsRelativeOrHttpsAddress(request.LinkUrl, allowAnchor: true)
        )
        {
            errors["linkUrl"] =
            ["Buton bağlantısı /, # veya HTTPS ile başlayan geçerli bir adres olmalıdır."];
        }

        return errors;
    }

    private static bool IsRelativeOrHttpsAddress(
        string address,
        bool allowAnchor
    )
    {
        var value = address.Trim();
        var isSafeRelativePath = value.StartsWith('/')
            && (
                value.Length == 1
                || (value[1] != '/' && value[1] != '\\')
            );
        if (
            isSafeRelativePath
            || (allowAnchor && value.StartsWith('#'))
        )
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }

    private ObjectResult? ValidateAdmin()
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return ProblemResponse(
                StatusCodes.Status401Unauthorized,
                "Oturum bulunamadı",
                "Yönetim panelini kullanmak için yeniden giriş yapın."
            );
        }

        return string.Equals(
            ticket.Role,
            "Admin",
            StringComparison.OrdinalIgnoreCase
        )
            ? null
            : ProblemResponse(
                StatusCodes.Status403Forbidden,
                "Yetkiniz bulunmuyor",
                "Bu işlem yalnızca yöneticiler tarafından yapılabilir."
            );
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

    private ObjectResult ProblemResponse(int status, string title, string detail)
    {
        return StatusCode(status, new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path,
        });
    }
}

public sealed class SaveHomeBannerRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 2)]
    public required string ImageUrl { get; init; }

    [Required]
    [StringLength(300, MinimumLength = 3)]
    public required string AltText { get; init; }

    [StringLength(200)]
    public string? Title { get; init; }

    [StringLength(750)]
    public string? Description { get; init; }

    [StringLength(100)]
    public string? ButtonText { get; init; }

    [StringLength(2000)]
    public string? LinkUrl { get; init; }

    [Range(0, 1000)]
    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; }

    public SaveHomeBannerCommand ToCommand() => new(
        ImageUrl,
        AltText,
        Title,
        Description,
        ButtonText,
        LinkUrl,
        DisplayOrder,
        IsActive
    );
}
