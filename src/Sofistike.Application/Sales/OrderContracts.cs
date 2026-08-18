namespace Sofistike.Application.Sales;

public sealed record CreateOrderCommand(
    string ContactEmail,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string City,
    string District,
    string AddressLine,
    string? PostalCode,
    string? AddressTitle,
    string DeliveryMethod
);

public sealed record CreatedOrder(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    decimal Subtotal,
    decimal ShippingAmount,
    decimal TotalAmount,
    string CurrencyCode,
    DateTimeOffset CreatedAtUtc
);

public enum CreateOrderStatus
{
    Created,
    EmptyCart,
    UserNotFound,
    ProductUnavailable,
}

public sealed record CreateOrderResult(
    CreateOrderStatus Status,
    CreatedOrder? Order,
    string? UnavailableProductName = null
);

public interface IOrderService
{
    Task<CreateOrderResult> CreateAsync(
        Guid userId,
        CreateOrderCommand command,
        CancellationToken cancellationToken = default
    );
}
