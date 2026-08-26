using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Sales;
using Sofistike.Domain.Catalog;
using Sofistike.Domain.Sales;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Sales;

public sealed class OrderService(SofistikeDbContext dbContext) : IOrderService
{
    private const decimal ExpressShippingAmount = 59m;

    public async Task<IReadOnlyList<OrderSummary>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var orders = await dbContext.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Items)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return orders.Select(MapSummary).ToList();
    }

    public async Task<CreateOrderResult> CreateAsync(
        Guid userId,
        CreateOrderCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId && item.IsActive,
            cancellationToken
        );
        if (user is null)
        {
            return new CreateOrderResult(CreateOrderStatus.UserNotFound, null);
        }

        var cart = await CartQuery().SingleOrDefaultAsync(
            item => item.UserId == userId,
            cancellationToken
        );
        if (cart is null || cart.Items.Count == 0)
        {
            return new CreateOrderResult(CreateOrderStatus.EmptyCart, null);
        }

        var now = DateTimeOffset.UtcNow;
        var preparedItems = new List<(CartItem CartItem, decimal Price, string Currency)>();
        foreach (var cartItem in cart.Items)
        {
            var variant = cartItem.ProductVariant;
            var product = variant.Product;
            var price = SalesPricing.GetEffectivePrice(variant, now);
            var availableQuantity = SalesPricing.GetAvailableQuantity(variant);
            if (
                product.Status != ProductStatus.Active
                || !variant.IsActive
                || price is null
                || availableQuantity < cartItem.Quantity
            )
            {
                return new CreateOrderResult(
                    CreateOrderStatus.ProductUnavailable,
                    null,
                    product.Name
                );
            }

            preparedItems.Add((cartItem, price.Value.Amount, price.Value.CurrencyCode));
        }

        var currencyCode = preparedItems[0].Currency;
        if (preparedItems.Any(item => item.Currency != currencyCode))
        {
            return new CreateOrderResult(
                CreateOrderStatus.ProductUnavailable,
                null,
                "Farklı para birimindeki ürünler"
            );
        }

        var deliveryMethod = string.Equals(
            command.DeliveryMethod,
            "express",
            StringComparison.OrdinalIgnoreCase
        )
            ? DeliveryMethod.Express
            : DeliveryMethod.Standard;
        var subtotal = preparedItems.Sum(item =>
            item.Price * item.CartItem.Quantity);
        var shippingAmount = deliveryMethod == DeliveryMethod.Express
            ? ExpressShippingAmount
            : 0m;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = CreateOrderNumber(now),
            UserId = userId,
            User = user,
            Status = OrderStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Pending,
            DeliveryMethod = deliveryMethod,
            ContactEmail = command.ContactEmail.Trim(),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            PhoneNumber = command.PhoneNumber.Trim(),
            City = command.City.Trim(),
            District = command.District.Trim(),
            AddressLine = command.AddressLine.Trim(),
            PostalCode = NormalizeOptional(command.PostalCode),
            AddressTitle = NormalizeOptional(command.AddressTitle),
            Subtotal = subtotal,
            ShippingAmount = shippingAmount,
            TotalAmount = subtotal + shippingAmount,
            CurrencyCode = currencyCode,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        foreach (var preparedItem in preparedItems)
        {
            var variant = preparedItem.CartItem.ProductVariant;
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Order = order,
                ProductId = variant.ProductId,
                ProductVariantId = variant.Id,
                ProductName = variant.Product.Name,
                VariantName = variant.Name,
                Sku = variant.Sku,
                UnitPrice = preparedItem.Price,
                Quantity = preparedItem.CartItem.Quantity,
                LineTotal = preparedItem.Price * preparedItem.CartItem.Quantity,
                CurrencyCode = preparedItem.Currency,
            });
        }

        dbContext.Orders.Add(order);
        dbContext.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateOrderResult(
            CreateOrderStatus.Created,
            new CreatedOrder(
                order.Id,
                order.OrderNumber,
                order.Status.ToString(),
                order.PaymentStatus.ToString(),
                order.Subtotal,
                order.ShippingAmount,
                order.TotalAmount,
                order.CurrencyCode,
                order.CreatedAtUtc
            )
        );
    }

    public async Task<CancelOrderStatus> CancelAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default
    )
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(
            item => item.Id == orderId && item.UserId == userId,
            cancellationToken
        );
        if (order is null)
        {
            return CancelOrderStatus.NotFound;
        }

        if (order.Status is not (
            OrderStatus.AwaitingPayment
            or OrderStatus.Confirmed
            or OrderStatus.Preparing
        ))
        {
            return CancelOrderStatus.NotCancellable;
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CancelOrderStatus.Cancelled;
    }

    private IQueryable<ShoppingCart> CartQuery()
    {
        return dbContext.ShoppingCarts
            .AsSplitQuery()
            .Include(cart => cart.Items)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant.Product)
            .Include(cart => cart.Items)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant.Prices)
            .Include(cart => cart.Items)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant.CampaignPrices)
                        .ThenInclude(price => price.Campaign)
            .Include(cart => cart.Items)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant.Stocks);
    }

    private static string CreateOrderNumber(DateTimeOffset now)
    {
        return $"SFX-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static OrderSummary MapSummary(Order order)
    {
        return new OrderSummary(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            order.DeliveryMethod.ToString(),
            order.ContactEmail,
            $"{order.FirstName} {order.LastName}".Trim(),
            order.City,
            order.District,
            order.AddressLine,
            order.Subtotal,
            order.ShippingAmount,
            order.TotalAmount,
            order.CurrencyCode,
            order.CreatedAtUtc,
            order.Items
                .OrderBy(item => item.ProductName)
                .Select(item => new OrderLineSummary(
                    item.Id,
                    item.ProductId,
                    item.ProductName,
                    item.VariantName,
                    item.Sku,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal,
                    item.CurrencyCode
                ))
                .ToList()
        );
    }
}
