using Microsoft.AspNetCore.Mvc;
using Sofistike.Application.Catalog;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(IProductCatalogService productCatalogService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategorySummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategorySummary>>> GetCategories(
        CancellationToken cancellationToken
    )
    {
        var categories = await productCatalogService.GetCategoriesAsync(
            cancellationToken
        );

        return Ok(categories);
    }
}
