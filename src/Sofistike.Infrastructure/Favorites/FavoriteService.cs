using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Catalog;
using Sofistike.Application.Favorites;
using Sofistike.Domain.Catalog;
using Sofistike.Domain.Favorites;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Favorites;

public sealed class FavoriteService(SofistikeDbContext dbContext)
    : IFavoriteService
{
    public async Task<PagedFavoriteResult> GetAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        var query = FavoriteQuery()
            .Where(favorite =>
                favorite.UserId == userId
                && favorite.Product.Status == ProductStatus.Active);
        var totalItems = await query.CountAsync(cancellationToken);
        var favorites = await query
            .OrderByDescending(favorite => favorite.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return new PagedFavoriteResult(
            favorites.Select(favorite => MapFavorite(favorite, now)).ToList(),
            page,
            pageSize,
            totalItems,
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)pageSize)
        );
    }

    public async Task<AddFavoriteResult> AddAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default
    )
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId && user.IsActive,
                cancellationToken
            );
        if (!userExists)
        {
            return new AddFavoriteResult(AddFavoriteStatus.UserNotFound, null);
        }

        var existing = await FavoriteQuery().SingleOrDefaultAsync(
            favorite => favorite.UserId == userId && favorite.ProductId == productId,
            cancellationToken
        );
        if (existing is not null)
        {
            return new AddFavoriteResult(
                AddFavoriteStatus.AlreadyExists,
                MapFavorite(existing, DateTimeOffset.UtcNow)
            );
        }

        var product = await ProductQuery().SingleOrDefaultAsync(
            item => item.Id == productId && item.Status == ProductStatus.Active,
            cancellationToken
        );
        if (product is null)
        {
            return new AddFavoriteResult(AddFavoriteStatus.ProductNotFound, null);
        }

        var favorite = new UserFavorite
        {
            UserId = userId,
            ProductId = productId,
            Product = product,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        dbContext.UserFavorites.Add(favorite);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(favorite).State = EntityState.Detached;
            existing = await FavoriteQuery().SingleOrDefaultAsync(
                item => item.UserId == userId && item.ProductId == productId,
                cancellationToken
            );
            if (existing is null)
            {
                throw;
            }

            return new AddFavoriteResult(
                AddFavoriteStatus.AlreadyExists,
                MapFavorite(existing, DateTimeOffset.UtcNow)
            );
        }

        return new AddFavoriteResult(
            AddFavoriteStatus.Added,
            MapFavorite(favorite, DateTimeOffset.UtcNow)
        );
    }

    public async Task RemoveAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default
    )
    {
        await dbContext.UserFavorites
            .Where(favorite =>
                favorite.UserId == userId && favorite.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private IQueryable<UserFavorite> FavoriteQuery()
    {
        return dbContext.UserFavorites
            .AsNoTracking()
            .AsSplitQuery()
            .Include(favorite => favorite.Product)
                .ThenInclude(product => product.ProductCategories)
                    .ThenInclude(item => item.Category)
            .Include(favorite => favorite.Product)
                .ThenInclude(product => product.Images)
            .Include(favorite => favorite.Product)
                .ThenInclude(product => product.Variants)
                    .ThenInclude(variant => variant.Prices)
            .Include(favorite => favorite.Product)
                .ThenInclude(product => product.Variants)
                    .ThenInclude(variant => variant.CampaignPrices)
                        .ThenInclude(price => price.Campaign)
            .Include(favorite => favorite.Product)
                .ThenInclude(product => product.Variants)
                    .ThenInclude(variant => variant.Stocks);
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

    private static FavoriteItem MapFavorite(
        UserFavorite favorite,
        DateTimeOffset now
    )
    {
        return new FavoriteItem(
            favorite.CreatedAtUtc,
            MapProduct(favorite.Product, now)
        );
    }

    private static ProductCard MapProduct(Product product, DateTimeOffset now)
    {
        var activeVariants = product.Variants.Where(variant => variant.IsActive).ToList();
        var price = activeVariants
            .Select(variant => MapPrice(variant, now))
            .Where(item => item is not null)
            .OrderBy(item => item!.EffectivePrice)
            .FirstOrDefault();
        var availableQuantity = activeVariants
            .SelectMany(variant => variant.Stocks)
            .Sum(stock => stock.OnHandQuantity - stock.ReservedQuantity);
        var stockStatus = availableQuantity switch
        {
            <= 0 => "OutOfStock",
            <= 3 => "LowStock",
            _ => "InStock",
        };
        var image = product.Images
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.DisplayOrder)
            .FirstOrDefault();

        return new ProductCard(
            product.Id,
            product.ProductCode,
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.IsPopular,
            product.IsXtra,
            image is null
                ? null
                : new ProductImageSummary(
                    image.Id,
                    image.VariantId,
                    image.ImageUrl,
                    image.AltText,
                    image.IsPrimary,
                    image.DisplayOrder
                ),
            price,
            new StockSummary(availableQuantity, stockStatus),
            product.ProductCategories
                .Where(item => item.Category.IsActive)
                .OrderByDescending(item => item.IsPrimary)
                .ThenBy(item => item.Category.DisplayOrder)
                .Select(item => new CategorySummary(
                    item.Category.Id,
                    item.Category.Name,
                    item.Category.Slug,
                    item.Category.Description,
                    item.Category.ParentCategory?.Slug,
                    item.Category.MenuGroup,
                    item.Category.DisplayOrder
                ))
                .ToList()
        );
    }

    private static ProductPriceSummary? MapPrice(
        ProductVariant variant,
        DateTimeOffset now
    )
    {
        var listPrice = variant.Prices
            .Where(price =>
                price.ValidFromUtc <= now
                && (price.ValidToUtc is null || price.ValidToUtc > now))
            .OrderByDescending(price => price.ValidFromUtc)
            .FirstOrDefault();
        if (listPrice is null)
        {
            return null;
        }

        var campaignPrice = variant.CampaignPrices
            .Where(price =>
                price.Campaign.IsActive
                && price.Campaign.StartsAtUtc <= now
                && price.Campaign.EndsAtUtc > now
                && price.DiscountedAmount < listPrice.Amount)
            .OrderBy(price => price.DiscountedAmount)
            .FirstOrDefault();
        var effectivePrice = campaignPrice?.DiscountedAmount ?? listPrice.Amount;
        var discountPercentage = campaignPrice is null
            ? null
            : (int?)decimal.Round(
                (listPrice.Amount - campaignPrice.DiscountedAmount)
                / listPrice.Amount
                * 100,
                0,
                MidpointRounding.AwayFromZero
            );

        return new ProductPriceSummary(
            listPrice.Amount,
            campaignPrice?.DiscountedAmount,
            effectivePrice,
            listPrice.CurrencyCode,
            discountPercentage
        );
    }
}
