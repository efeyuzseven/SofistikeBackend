using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Favorites;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/account/favorites")]
public sealed class FavoritesController(
    IFavoriteService favoriteService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedFavoriteResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedFavoriteResult>> GetFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem();
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["pagination"] =
                    ["Sayfa numarası en az 1, sayfa boyutu 1 ile 100 arasında olmalıdır."],
                }
            ));
        }

        var favorites = await favoriteService.GetAsync(
            ticket.UserId,
            page,
            pageSize,
            cancellationToken
        );

        return Ok(favorites);
    }

    [HttpPost("{productId:guid}")]
    [ProducesResponseType<FavoriteItem>(StatusCodes.Status201Created)]
    [ProducesResponseType<FavoriteItem>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FavoriteItem>> AddFavorite(
        Guid productId,
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem<FavoriteItem>();
        }

        var result = await favoriteService.AddAsync(
            ticket.UserId,
            productId,
            cancellationToken
        );

        return result.Status switch
        {
            AddFavoriteStatus.Added => StatusCode(
                StatusCodes.Status201Created,
                result.Favorite
            ),
            AddFavoriteStatus.AlreadyExists => Ok(result.Favorite),
            AddFavoriteStatus.UserNotFound => UnauthorizedProblem<FavoriteItem>(),
            _ => NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ürün bulunamadı",
                Detail = "Favorilere eklenmek istenen ürün bulunamadı veya satışa açık değil.",
                Instance = HttpContext.Request.Path,
            }),
        };
    }

    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFavorite(
        Guid productId,
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem();
        }

        await favoriteService.RemoveAsync(
            ticket.UserId,
            productId,
            cancellationToken
        );

        return NoContent();
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

    private UnauthorizedObjectResult UnauthorizedProblem()
    {
        return Unauthorized(CreateUnauthorizedProblem());
    }

    private ActionResult<T> UnauthorizedProblem<T>()
    {
        return Unauthorized(CreateUnauthorizedProblem());
    }

    private ProblemDetails CreateUnauthorizedProblem()
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Oturum bulunamadı",
            Detail = "Favorilerinize erişmek için yeniden giriş yapın.",
            Instance = HttpContext.Request.Path,
        };
    }
}
