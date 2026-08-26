using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Catalog;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
public sealed class AdminProductsController(
    IProductManagementService productManagementService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ProductDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct(
        CreateProductRequest request,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var result = await productManagementService.CreateAsync(
            new CreateProductCommand(
                request.ProductCode,
                request.Name,
                request.Slug,
                request.ShortDescription,
                request.Description,
                request.ResolveCategoryIds(),
                request.Price,
                request.StockQuantity,
                request.ImageUrl,
                request.IsPopular,
                request.IsXtra
            ),
            cancellationToken
        );

        return result.Status switch
        {
            CreateProductStatus.Created => StatusCode(
                StatusCodes.Status201Created,
                result.Product
            ),
            CreateProductStatus.CategoryNotFound => ProblemResponse(
                StatusCodes.Status400BadRequest,
                "Kategori bulunamadı",
                "Seçilen kategori bulunamadı veya kullanıma açık değil."
            ),
            CreateProductStatus.DuplicateProductCode => ProblemResponse(
                StatusCodes.Status409Conflict,
                "Ürün kodu kullanımda",
                "Bu ürün koduyla daha önce bir ürün oluşturulmuş."
            ),
            _ => ProblemResponse(
                StatusCodes.Status409Conflict,
                "Ürün adresi kullanımda",
                "Bu ürün bağlantısıyla daha önce bir ürün oluşturulmuş."
            ),
        };
    }

    [HttpPut("{productId:guid}")]
    [ProducesResponseType<ProductDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProduct(
        Guid productId,
        CreateProductRequest request,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var result = await productManagementService.UpdateAsync(
            productId,
            new UpdateProductCommand(
                request.ProductCode,
                request.Name,
                request.Slug,
                request.ShortDescription,
                request.Description,
                request.ResolveCategoryIds(),
                request.Price,
                request.StockQuantity,
                request.ImageUrl,
                request.IsPopular,
                request.IsXtra
            ),
            cancellationToken
        );

        return result.Status switch
        {
            UpdateProductStatus.Updated => Ok(result.Product),
            UpdateProductStatus.NotFound => ProblemResponse(
                StatusCodes.Status404NotFound,
                "Ürün bulunamadı",
                "Güncellenmek istenen ürün bulunamadı veya arşivlenmiş."
            ),
            UpdateProductStatus.CategoryNotFound => ProblemResponse(
                StatusCodes.Status400BadRequest,
                "Kategori bulunamadı",
                "Seçilen kategori bulunamadı veya kullanıma açık değil."
            ),
            UpdateProductStatus.DefaultVariantNotFound => ProblemResponse(
                StatusCodes.Status409Conflict,
                "Ürün varyantı bulunamadı",
                "Ürünün fiyat ve stok bilgilerinin bağlı olduğu varyant bulunamadı."
            ),
            UpdateProductStatus.DuplicateProductCode => ProblemResponse(
                StatusCodes.Status409Conflict,
                "Ürün kodu kullanımda",
                "Bu ürün koduyla daha önce bir ürün oluşturulmuş."
            ),
            UpdateProductStatus.DuplicateSlug => ProblemResponse(
                StatusCodes.Status409Conflict,
                "Ürün adresi kullanımda",
                "Bu ürün bağlantısıyla daha önce bir ürün oluşturulmuş."
            ),
            _ => ProblemResponse(
                StatusCodes.Status409Conflict,
                "Ürün başka bir işlemde güncellendi",
                "Son verileri yükleyip değişikliklerinizi yeniden deneyin."
            ),
        };
    }

    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchiveProduct(
        Guid productId,
        CancellationToken cancellationToken
    )
    {
        var authorizationResult = ValidateAdmin();
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var status = await productManagementService.ArchiveAsync(
            productId,
            cancellationToken
        );
        return status switch
        {
            ArchiveProductStatus.Archived => NoContent(),
            ArchiveProductStatus.NotFound => ProblemResponse(
                StatusCodes.Status404NotFound,
                "Ürün bulunamadı",
                "Arşivlenmek istenen ürün bulunamadı."
            ),
            _ => ProblemResponse(
                StatusCodes.Status409Conflict,
                "Ürün başka bir işlemde güncellendi",
                "Son verileri yükleyip arşivleme işlemini yeniden deneyin."
            ),
        };
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

    private ObjectResult ProblemResponse(
        int status,
        string title,
        string detail
    )
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

public sealed class CreateProductRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public required string ProductCode { get; init; }

    [Required]
    [StringLength(250, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    [StringLength(280, MinimumLength = 2)]
    public required string Slug { get; init; }

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public required string ShortDescription { get; init; }

    [Required]
    [StringLength(5000, MinimumLength = 3)]
    public required string Description { get; init; }

    public Guid? CategoryId { get; init; }

    [MinLength(1)]
    public Guid[]? CategoryIds { get; init; }

    [Range(
        typeof(decimal),
        "0.01",
        "99999999",
        ParseLimitsInInvariantCulture = true
    )]
    public decimal Price { get; init; }

    [Range(0, 1000000)]
    public int StockQuantity { get; init; }

    [StringLength(2000)]
    public string? ImageUrl { get; init; }

    public bool IsPopular { get; init; }

    public bool IsXtra { get; init; }

    public IReadOnlyList<Guid> ResolveCategoryIds()
    {
        if (CategoryIds is { Length: > 0 })
        {
            return CategoryIds;
        }

        return CategoryId.HasValue ? [CategoryId.Value] : [];
    }
}
