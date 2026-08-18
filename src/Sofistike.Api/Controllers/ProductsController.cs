using Microsoft.AspNetCore.Mvc;
using Sofistike.Application.Catalog;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(IProductCatalogService productCatalogService)
    : ControllerBase
{
    private static readonly Dictionary<string, ProductSort> SortOptions =
        new Dictionary<string, ProductSort>(StringComparer.OrdinalIgnoreCase)
        {
            ["recommended"] = ProductSort.Recommended,
            ["newest"] = ProductSort.Newest,
            ["price-asc"] = ProductSort.PriceAscending,
            ["price-desc"] = ProductSort.PriceDescending,
            ["name-asc"] = ProductSort.NameAscending,
        };

    [HttpGet]
    [ProducesResponseType<PagedProductResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedProductResult>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] bool? isPopular,
        [FromQuery] bool? isXtra,
        [FromQuery] bool? inStock,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string sort = "recommended",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        var errors = ValidateQuery(
            search,
            minPrice,
            maxPrice,
            sort,
            page,
            pageSize
        );
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var result = await productCatalogService.GetProductsAsync(
            new ProductListQuery(
                category,
                search,
                isPopular,
                isXtra,
                inStock,
                minPrice,
                maxPrice,
                SortOptions[sort],
                page,
                pageSize
            ),
            cancellationToken
        );

        return Ok(result);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType<ProductDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetails>> GetProduct(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var product = await productCatalogService.GetProductBySlugAsync(
            slug,
            cancellationToken
        );

        if (product is not null)
        {
            return Ok(product);
        }

        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Ürün bulunamadı",
            Detail = "İstenen ürün bulunamadı veya satışa açık değil.",
            Instance = HttpContext.Request.Path,
        });
    }

    private static Dictionary<string, string[]> ValidateQuery(
        string? search,
        decimal? minPrice,
        decimal? maxPrice,
        string sort,
        int page,
        int pageSize
    )
    {
        var errors = new Dictionary<string, string[]>();

        if (search?.Length > 100)
        {
            errors["search"] = ["Arama metni en fazla 100 karakter olabilir."];
        }

        if (minPrice < 0 || maxPrice < 0)
        {
            errors["price"] = ["Fiyat değerleri sıfırdan küçük olamaz."];
        }
        else if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
        {
            errors["price"] = ["Minimum fiyat maksimum fiyattan büyük olamaz."];
        }

        if (!SortOptions.ContainsKey(sort))
        {
            errors["sort"] =
            ["Sıralama recommended, newest, price-asc, price-desc veya name-asc olmalıdır."];
        }

        if (page < 1)
        {
            errors["page"] = ["Sayfa numarası en az 1 olmalıdır."];
        }

        if (pageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["Sayfa boyutu 1 ile 100 arasında olmalıdır."];
        }

        return errors;
    }
}
