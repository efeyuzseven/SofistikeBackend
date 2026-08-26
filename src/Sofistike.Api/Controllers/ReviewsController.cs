using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Reviews;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/account/reviews")]
public sealed class ReviewsController(
    IReviewService reviewService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReviewSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ReviewSummary>>> GetReviews(
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem<IReadOnlyList<ReviewSummary>>();
        }

        return Ok(await reviewService.GetAsync(ticket.UserId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<ReviewSummary>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewSummary>> CreateReview(
        CreateReviewRequest request,
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem<ReviewSummary>();
        }

        var result = await reviewService.CreateAsync(
            ticket.UserId,
            new CreateReviewCommand(
                request.OrderId,
                request.ProductId,
                request.Rating,
                request.Comment,
                request.IsAnonymous
            ),
            cancellationToken
        );

        return result.Status switch
        {
            CreateReviewStatus.Created => StatusCode(
                StatusCodes.Status201Created,
                result.Review
            ),
            CreateReviewStatus.OrderNotFound => NotFound(CreateProblem(
                StatusCodes.Status404NotFound,
                "Sipariş bulunamadı",
                "Değerlendirmek istediğiniz sipariş hesabınızda bulunamadı."
            )),
            CreateReviewStatus.ProductNotInOrder => Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                "Ürün siparişte bulunamadı",
                "Yalnızca satın aldığınız ürünleri değerlendirebilirsiniz."
            )),
            _ => Conflict(CreateProblem(
                StatusCodes.Status409Conflict,
                "Değerlendirme mevcut",
                "Bu sipariş veya ürün için daha önce değerlendirme gönderdiniz."
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
            "Değerlendirmelerinize erişmek için yeniden giriş yapın."
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

public sealed class CreateReviewRequest
{
    public Guid OrderId { get; init; }

    public Guid? ProductId { get; init; }

    [Range(1, 5)]
    public int Rating { get; init; }

    [Required]
    [StringLength(2000, MinimumLength = 3)]
    public required string Comment { get; init; }

    public bool IsAnonymous { get; init; }
}
