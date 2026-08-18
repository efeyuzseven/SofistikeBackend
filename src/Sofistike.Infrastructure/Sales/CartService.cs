using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Sales;
using Sofistike.Domain.Catalog;
using Sofistike.Domain.Sales;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Sales;

public sealed class CartService(SofistikeDbContext dbContext) : ICartService
{
    public async Task<CartSummary> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var cart = await CartQuery(asTracking: false).SingleOrDefaultAsync(
            item => item.UserId == userId,
            cancellationToken
        );

        return cart is null
            ? EmptyCart()
            : MapCart(cart, DateTimeOffset.UtcNow);
    }

    public Task<CartMutationResult> AddAsync(
        Guid userId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default
    )
    {
        return AddCoreAsync(
            userId,
            productId,
            quantity,
            retryOnConcurrencyConflict: true,
            cancellationToken
        );
    }

    private async Task<CartMutationResult> AddCoreAsync(
        Guid userId,
        Guid productId,
        int quantity,
        bool retryOnConcurrencyConflict,
        CancellationToken cancellationToken
    )
    {
        var userExists = await dbContext.Users.AnyAsync(
            user => user.Id == userId && user.IsActive,
            cancellationToken
        );
        if (!userExists)
        {
            return new CartMutationResult(CartMutationStatus.UserNotFound, null);
        }

        var product = await ProductQuery().SingleOrDefaultAsync(
            item => item.Id == productId,
            cancellationToken
        );
        if (product is null || product.Status != ProductStatus.Active)
        {
            return new CartMutationResult(CartMutationStatus.ProductNotFound, null);
        }

        var variant = product.Variants
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.CreatedAtUtc)
            .FirstOrDefault();
        var now = DateTimeOffset.UtcNow;
        var price = variant is null
            ? null
            : SalesPricing.GetEffectivePrice(variant, now);
        if (variant is null || price is null)
        {
            return new CartMutationResult(
                CartMutationStatus.ProductUnavailable,
                null
            );
        }

        var availableQuantity = SalesPricing.GetAvailableQuantity(variant);
        if (availableQuantity < 1)
        {
            return new CartMutationResult(
                CartMutationStatus.ProductUnavailable,
                null
            );
        }

        var cart = await CartQuery(asTracking: true).SingleOrDefaultAsync(
            item => item.UserId == userId,
            cancellationToken
        );
        if (cart is null)
        {
            cart = new ShoppingCart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            dbContext.ShoppingCarts.Add(cart);
        }

        var existing = cart.Items.SingleOrDefault(item =>
            item.ProductVariantId == variant.Id);
        var requestedQuantity = (existing?.Quantity ?? 0) + quantity;
        if (requestedQuantity > 9 || requestedQuantity > availableQuantity)
        {
            return new CartMutationResult(
                CartMutationStatus.QuantityUnavailable,
                MapCart(cart, now)
            );
        }

        if (existing is null)
        {
            var cartItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                Cart = cart,
                ProductVariantId = variant.Id,
                ProductVariant = variant,
                Quantity = quantity,
                AddedAtUtc = now,
                UpdatedAtUtc = now,
            };
            cart.Items.Add(cartItem);
            dbContext.CartItems.Add(cartItem);
        }
        else
        {
            existing.Quantity = requestedQuantity;
            existing.UpdatedAtUtc = now;
        }

        cart.UpdatedAtUtc = now;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException) when (retryOnConcurrencyConflict)
        {
            dbContext.ChangeTracker.Clear();
            return await AddCoreAsync(
                userId,
                productId,
                quantity,
                retryOnConcurrencyConflict: false,
                cancellationToken
            );
        }

        return new CartMutationResult(
            CartMutationStatus.Success,
            MapCart(cart, now)
        );
    }

    public async Task<CartMutationResult> UpdateAsync(
        Guid userId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default
    )
    {
        var cart = await CartQuery(asTracking: true).SingleOrDefaultAsync(
            item => item.UserId == userId,
            cancellationToken
        );
        if (cart is null)
        {
            return new CartMutationResult(
                CartMutationStatus.CartItemNotFound,
                EmptyCart()
            );
        }

        var item = cart.Items.SingleOrDefault(line =>
            line.ProductVariant.ProductId == productId);
        if (item is null)
        {
            return new CartMutationResult(
                CartMutationStatus.CartItemNotFound,
                MapCart(cart, DateTimeOffset.UtcNow)
            );
        }

        var availableQuantity = SalesPricing.GetAvailableQuantity(
            item.ProductVariant
        );
        if (quantity > availableQuantity)
        {
            return new CartMutationResult(
                CartMutationStatus.QuantityUnavailable,
                MapCart(cart, DateTimeOffset.UtcNow)
            );
        }

        var now = DateTimeOffset.UtcNow;
        item.Quantity = quantity;
        item.UpdatedAtUtc = now;
        cart.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CartMutationResult(
            CartMutationStatus.Success,
            MapCart(cart, now)
        );
    }

    public async Task<CartSummary> RemoveAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default
    )
    {
        var cart = await CartQuery(asTracking: true).SingleOrDefaultAsync(
            item => item.UserId == userId,
            cancellationToken
        );
        if (cart is null)
        {
            return EmptyCart();
        }

        var item = cart.Items.SingleOrDefault(line =>
            line.ProductVariant.ProductId == productId);
        if (item is not null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapCart(cart, DateTimeOffset.UtcNow);
    }

    private IQueryable<ShoppingCart> CartQuery(bool asTracking)
    {
        var query = dbContext.ShoppingCarts
            .AsSplitQuery()
            .Include(cart => cart.Items)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant.Product)
                        .ThenInclude(product => product.ProductCategories)
                            .ThenInclude(item => item.Category)
            .Include(cart => cart.Items)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant.Product)
                        .ThenInclude(product => product.Images)
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

        return asTracking ? query : query.AsNoTracking();
    }

    private IQueryable<Product> ProductQuery()
    {
        return dbContext.Products
            .AsSplitQuery()
            .Include(product => product.ProductCategories)
                .ThenInclude(item => item.Category)
            .Include(product => product.Images)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Prices)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.CampaignPrices)
                    .ThenInclude(price => price.Campaign)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Stocks);
    }

    private static CartSummary MapCart(
        ShoppingCart cart,
        DateTimeOffset now
    )
    {
        var items = cart.Items
            .Where(item =>
                item.ProductVariant.IsActive
                && item.ProductVariant.Product.Status == ProductStatus.Active)
            .Select(item => MapItem(item, now))
            .Where(item => item is not null)
            .Cast<CartLineItem>()
            .OrderBy(item => item.AddedAtUtc)
            .ToList();
        var currency = items.FirstOrDefault()?.Product.CurrencyCode ?? "TRY";

        return new CartSummary(
            items,
            items.Sum(item => item.Quantity),
            items.Sum(item => item.LineTotal),
            currency,
            cart.UpdatedAtUtc
        );
    }

    private static CartLineItem? MapItem(CartItem item, DateTimeOffset now)
    {
        var variant = item.ProductVariant;
        var product = variant.Product;
        var price = SalesPricing.GetEffectivePrice(variant, now);
        if (price is null)
        {
            return null;
        }

        var category = product.ProductCategories
            .Where(link => link.Category.IsActive)
            .OrderByDescending(link => link.IsPrimary)
            .ThenBy(link => link.Category.DisplayOrder)
            .Select(link => link.Category.Name)
            .FirstOrDefault() ?? "Sofistike";
        var image = product.Images
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.DisplayOrder)
            .Select(item => item.ImageUrl)
            .FirstOrDefault();

        return new CartLineItem(
            new CartProductSummary(
                product.Id,
                variant.Id,
                product.Name,
                variant.Name,
                variant.Sku,
                category,
                image,
                price.Value.Amount,
                price.Value.CurrencyCode,
                SalesPricing.GetAvailableQuantity(variant)
            ),
            item.Quantity,
            price.Value.Amount * item.Quantity,
            item.AddedAtUtc
        );
    }

    private static CartSummary EmptyCart()
    {
        return new CartSummary([], 0, 0m, "TRY", null);
    }
}
