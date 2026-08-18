using Sofistike.Domain.Catalog;
using Sofistike.Domain.Users;

namespace Sofistike.Domain.Sales;

public enum OrderStatus
{
    AwaitingPayment,
    Confirmed,
    Preparing,
    Shipped,
    Delivered,
    Cancelled,
    Returned,
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded,
}

public enum DeliveryMethod
{
    Standard,
    Express,
}

public sealed class ShoppingCart
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserAccount User { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<CartItem> Items { get; set; } = [];
}

public sealed class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public ShoppingCart Cart { get; set; } = null!;
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTimeOffset AddedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class Order
{
    public Guid Id { get; set; }
    public required string OrderNumber { get; set; }
    public Guid UserId { get; set; }
    public UserAccount User { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public required string ContactEmail { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string City { get; set; }
    public required string District { get; set; }
    public required string AddressLine { get; set; }
    public string? PostalCode { get; set; }
    public string? AddressTitle { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public required string CurrencyCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Guid ProductVariantId { get; set; }
    public required string ProductName { get; set; }
    public required string VariantName { get; set; }
    public required string Sku { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public required string CurrencyCode { get; set; }
}
