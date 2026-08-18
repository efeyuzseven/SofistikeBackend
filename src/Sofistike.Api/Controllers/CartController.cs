using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Sales;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/account/cart")]
public sealed class CartController(
    ICartService cartService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CartSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartSummary>> GetCart(
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem<CartSummary>();
        }

        return Ok(await cartService.GetAsync(ticket.UserId, cancellationToken));
    }

    [HttpPost("items")]
    [ProducesResponseType<CartSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartSummary>> AddItem(
        CartItemRequest request,
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem<CartSummary>();
        }

        if (request.ProductId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.ProductId), "Ürün kimliği zorunludur.");
            return ValidationProblem(ModelState);
        }

        var result = await cartService.AddAsync(
            ticket.UserId,
            request.ProductId,
            request.Quantity,
            cancellationToken
        );

        return MapMutationResult(result);
    }

    [HttpPatch("items/{productId:guid}")]
    [ProducesResponseType<CartSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartSummary>> UpdateItem(
        Guid productId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem<CartSummary>();
        }

        var result = await cartService.UpdateAsync(
            ticket.UserId,
            productId,
            request.Quantity,
            cancellationToken
        );

        return MapMutationResult(result);
    }

    [HttpDelete("items/{productId:guid}")]
    [ProducesResponseType<CartSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartSummary>> RemoveItem(
        Guid productId,
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem<CartSummary>();
        }

        return Ok(await cartService.RemoveAsync(
            ticket.UserId,
            productId,
            cancellationToken
        ));
    }

    private ActionResult<CartSummary> MapMutationResult(
        CartMutationResult result
    )
    {
        return result.Status switch
        {
            CartMutationStatus.Success => Ok(result.Cart),
            CartMutationStatus.UserNotFound => UnauthorizedProblem<CartSummary>(),
            CartMutationStatus.ProductNotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Ürün bulunamadı",
                "Sepete eklenmek istenen ürün bulunamadı veya satışa açık değil."
            )),
            CartMutationStatus.CartItemNotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Sepet ürünü bulunamadı",
                "Güncellenmek istenen ürün sepette bulunamadı."
            )),
            CartMutationStatus.QuantityUnavailable => Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                "Yeterli stok bulunmuyor",
                "İstenen ürün adedi mevcut stok sınırını aşıyor."
            )),
            _ => Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                "Ürün satışa uygun değil",
                "Ürün şu anda sepete eklenemiyor."
            )),
        };
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

    private ActionResult<T> UnauthorizedProblem<T>()
    {
        return Unauthorized(CreateProblem(
            StatusCodes.Status401Unauthorized,
            "Oturum bulunamadı",
            "Sepetinize erişmek için yeniden giriş yapın."
        ));
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

public sealed class CartItemRequest
{
    public Guid ProductId { get; init; }

    [Range(1, 9)]
    public int Quantity { get; init; } = 1;
}

public sealed class UpdateCartItemRequest
{
    [Range(1, 9)]
    public int Quantity { get; init; }
}
