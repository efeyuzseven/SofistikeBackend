using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Sofistike.Api.Authentication;
using Sofistike.Application.Sales;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/account/orders")]
public sealed class OrdersController(
    IOrderService orderService,
    ISessionTicketService sessionTicketService
) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreatedOrder>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedOrder>> CreateOrder(
        CreateOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var ticket = ReadSessionTicket();
        if (ticket is null)
        {
            return UnauthorizedProblem();
        }

        if (
            !string.Equals(request.DeliveryMethod, "standard", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.DeliveryMethod, "express", StringComparison.OrdinalIgnoreCase)
        )
        {
            ModelState.AddModelError(
                nameof(request.DeliveryMethod),
                "Teslimat yöntemi standard veya express olmalıdır."
            );
            return ValidationProblem(ModelState);
        }

        var result = await orderService.CreateAsync(
            ticket.UserId,
            new CreateOrderCommand(
                request.ContactEmail,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.City,
                request.District,
                request.AddressLine,
                request.PostalCode,
                request.AddressTitle,
                request.DeliveryMethod
            ),
            cancellationToken
        );

        return result.Status switch
        {
            CreateOrderStatus.Created => StatusCode(
                StatusCodes.Status201Created,
                result.Order
            ),
            CreateOrderStatus.UserNotFound => UnauthorizedProblem(),
            CreateOrderStatus.EmptyCart => Conflict(CreateProblem(
                "Sepetiniz boş",
                "Sipariş oluşturmak için sepetinizde en az bir ürün bulunmalıdır."
            )),
            _ => Conflict(CreateProblem(
                "Ürün kullanılamıyor",
                $"{result.UnavailableProductName ?? "Sepetinizdeki bir ürün"} için fiyat veya stok doğrulanamadı."
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

    private ActionResult<CreatedOrder> UnauthorizedProblem()
    {
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Oturum bulunamadı",
            Detail = "Sipariş oluşturmak için yeniden giriş yapın.",
            Instance = HttpContext.Request.Path,
        });
    }

    private ProblemDetails CreateProblem(string title, string detail)
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = title,
            Detail = detail,
            Instance = HttpContext.Request.Path,
        };
    }
}

public sealed class CreateOrderRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public required string ContactEmail { get; init; }

    [Required]
    [StringLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [StringLength(100)]
    public required string LastName { get; init; }

    [Required]
    [StringLength(30)]
    public required string PhoneNumber { get; init; }

    [Required]
    [StringLength(100)]
    public required string City { get; init; }

    [Required]
    [StringLength(120)]
    public required string District { get; init; }

    [Required]
    [StringLength(1000)]
    public required string AddressLine { get; init; }

    [StringLength(20)]
    public string? PostalCode { get; init; }

    [StringLength(100)]
    public string? AddressTitle { get; init; }

    [Required]
    [StringLength(20)]
    public required string DeliveryMethod { get; init; }
}
