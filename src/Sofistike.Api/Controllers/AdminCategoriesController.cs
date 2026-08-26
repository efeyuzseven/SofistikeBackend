using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Catalog;
using Sofistike.Domain.Catalog;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/admin/categories")]
public sealed class AdminCategoriesController(
    ICategoryManagementService categoryManagementService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ManagedCategoryDetails>>(
        StatusCodes.Status200OK
    )]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCategories(
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        return Ok(await categoryManagementService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<ManagedCategoryDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        SaveCategoryRequest request,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var result = await categoryManagementService.CreateAsync(
            request.ToCommand(),
            cancellationToken
        );
        return result.Status == SaveCategoryStatus.Saved
            ? StatusCode(StatusCodes.Status201Created, result.Category)
            : DuplicateSlugResponse();
    }

    [HttpPut("{categoryId:guid}")]
    [ProducesResponseType<ManagedCategoryDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategory(
        Guid categoryId,
        SaveCategoryRequest request,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var result = await categoryManagementService.UpdateAsync(
            categoryId,
            request.ToCommand(),
            cancellationToken
        );
        return result.Status switch
        {
            SaveCategoryStatus.Saved => Ok(result.Category),
            SaveCategoryStatus.NotFound => ProblemResponse(
                StatusCodes.Status404NotFound,
                "Kategori bulunamadı",
                "Güncellenmek istenen kategori bulunamadı."
            ),
            _ => DuplicateSlugResponse(),
        };
    }

    [HttpDelete("{categoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(
        Guid categoryId,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var status = await categoryManagementService.DeleteAsync(
            categoryId,
            cancellationToken
        );
        return status switch
        {
            DeleteCategoryStatus.Deleted => NoContent(),
            DeleteCategoryStatus.NotFound => ProblemResponse(
                StatusCodes.Status404NotFound,
                "Kategori bulunamadı",
                "Silinmek istenen kategori bulunamadı."
            ),
            _ => ProblemResponse(
                StatusCodes.Status409Conflict,
                "Kategori kullanımda",
                "Ürünlere veya alt kategorilere bağlı kategoriler silinemez. Önce kategoriyi gizleyebilirsiniz."
            ),
        };
    }

    private ObjectResult DuplicateSlugResponse() => ProblemResponse(
        StatusCodes.Status409Conflict,
        "Kategori adresi kullanımda",
        "Bu bağlantı adıyla daha önce bir kategori oluşturulmuş."
    );

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

public sealed class SaveCategoryRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    [StringLength(180, MinimumLength = 2)]
    public required string Slug { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    [Required]
    public CategoryMenuGroup? MenuGroup { get; init; }

    [Range(0, 1000)]
    public int DisplayOrder { get; init; }

    public bool IsActive { get; init; }

    public SaveCategoryCommand ToCommand() => new(
        Name,
        Slug,
        Description,
        MenuGroup.GetValueOrDefault(),
        DisplayOrder,
        IsActive
    );
}
