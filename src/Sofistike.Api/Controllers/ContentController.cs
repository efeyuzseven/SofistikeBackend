using Microsoft.AspNetCore.Mvc;
using Sofistike.Application.Content;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/content")]
public sealed class ContentController(IHomeBannerService homeBannerService)
    : ControllerBase
{
    [HttpGet("banners")]
    [ProducesResponseType<IReadOnlyList<HomeBannerDetails>>(
        StatusCodes.Status200OK
    )]
    public async Task<ActionResult<IReadOnlyList<HomeBannerDetails>>> GetBanners(
        CancellationToken cancellationToken
    )
    {
        return Ok(await homeBannerService.GetActiveAsync(cancellationToken));
    }
}
