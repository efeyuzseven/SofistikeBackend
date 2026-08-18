namespace Sofistike.Application.Sales;

public sealed record CartProductSummary(
    Guid ProductId,
    Guid VariantId,
    string Name,
    string VariantName,
    string Sku,
    string Category,
    string? ImageUrl,
    decimal UnitPrice,
    string CurrencyCode,
    int AvailableQuantity
);

public sealed record CartLineItem(
    CartProductSummary Product,
    int Quantity,
    decimal LineTotal,
    DateTimeOffset AddedAtUtc
);

public sealed record CartSummary(
    IReadOnlyList<CartLineItem> Items,
    int ItemCount,
    decimal Subtotal,
    string CurrencyCode,
    DateTimeOffset? UpdatedAtUtc
);

public enum CartMutationStatus
{
    Success,
    UserNotFound,
    ProductNotFound,
    ProductUnavailable,
    QuantityUnavailable,
    CartItemNotFound,
}

public sealed record CartMutationResult(
    CartMutationStatus Status,
    CartSummary? Cart
);

public interface ICartService
{
    Task<CartSummary> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<CartMutationResult> AddAsync(
        Guid userId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default
    );

    Task<CartMutationResult> UpdateAsync(
        Guid userId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default
    );

    Task<CartSummary> RemoveAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default
    );
}
