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

public sealed record OrderLineSummary(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string VariantName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string CurrencyCode
);

public sealed record OrderSummary(
    Guid Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    string DeliveryMethod,
    string ContactEmail,
    string RecipientName,
    string City,
    string District,
    string AddressLine,
    decimal Subtotal,
    decimal ShippingAmount,
    decimal TotalAmount,
    string CurrencyCode,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<OrderLineSummary> Items
);

public enum CancelOrderStatus
{
    Cancelled,
    NotFound,
    NotCancellable,
}

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummary>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<CreateOrderResult> CreateAsync(
        Guid userId,
        CreateOrderCommand command,
        CancellationToken cancellationToken = default
    );

    Task<CancelOrderStatus> CancelAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default
    );
}
