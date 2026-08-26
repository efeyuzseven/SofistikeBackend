using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Catalog;
using Sofistike.Domain.Catalog;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Catalog;

public sealed class ProductCatalogService(SofistikeDbContext dbContext)
    : IProductCatalogService
{
    public async Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategorySummary(
                category.Id,
                category.Name,
                category.Slug,
                category.Description,
                category.ParentCategory == null
                    ? null
                    : category.ParentCategory.Slug,
                category.MenuGroup,
                category.DisplayOrder
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedProductResult> GetProductsAsync(
        ProductListQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var now = DateTimeOffset.UtcNow;
        var products = dbContext.Products
            .AsNoTracking()
            .Where(product => product.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            products = products.Where(product =>
                product.ProductCategories.Any(item =>
                    item.Category.IsActive && item.Category.Slug == category));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(product =>
                product.Name.Contains(search)
                || product.ProductCode.Contains(search)
                || product.ShortDescription.Contains(search)
                || product.Variants.Any(variant => variant.Sku.Contains(search)));
        }

        if (query.IsPopular.HasValue)
        {
            products = products.Where(product =>
                product.IsPopular == query.IsPopular.Value);
        }

        if (query.IsXtra.HasValue)
        {
            products = products.Where(product =>
                product.IsXtra == query.IsXtra.Value);
        }

        if (query.InStock.HasValue)
        {
            products = query.InStock.Value
                ? products.Where(product => product.Variants.Any(variant =>
                    variant.IsActive
                    && variant.Stocks.Any(stock =>
                        stock.OnHandQuantity > stock.ReservedQuantity)))
                : products.Where(product => !product.Variants.Any(variant =>
                    variant.IsActive
                    && variant.Stocks.Any(stock =>
                        stock.OnHandQuantity > stock.ReservedQuantity)));
        }

        var projected = products.Select(product => new ProductListingProjection
        {
            Id = product.Id,
            Name = product.Name,
            IsPopular = product.IsPopular,
            IsXtra = product.IsXtra,
            PublishedAtUtc = product.PublishedAtUtc,
            MinimumPrice = product.Variants
                .Where(variant => variant.IsActive)
                .Select(variant =>
                    variant.CampaignPrices
                        .Where(price =>
                            price.Campaign.IsActive
                            && price.Campaign.StartsAtUtc <= now
                            && price.Campaign.EndsAtUtc > now)
                        .Select(price => (decimal?)price.DiscountedAmount)
                        .Min()
                    ?? variant.Prices
                        .Where(price =>
                            price.ValidFromUtc <= now
                            && (price.ValidToUtc == null || price.ValidToUtc > now))
                        .Select(price => (decimal?)price.Amount)
                        .Min())
                .Min(),
        });

        if (query.MinimumPrice.HasValue)
        {
            projected = projected.Where(product =>
                product.MinimumPrice >= query.MinimumPrice.Value);
        }

        if (query.MaximumPrice.HasValue)
        {
            projected = projected.Where(product =>
                product.MinimumPrice <= query.MaximumPrice.Value);
        }

        projected = query.Sort switch
        {
            ProductSort.Newest => projected
                .OrderByDescending(product => product.PublishedAtUtc)
                .ThenBy(product => product.Name),
            ProductSort.PriceAscending => projected
                .OrderBy(product => product.MinimumPrice == null)
                .ThenBy(product => product.MinimumPrice)
                .ThenBy(product => product.Name),
            ProductSort.PriceDescending => projected
                .OrderBy(product => product.MinimumPrice == null)
                .ThenByDescending(product => product.MinimumPrice)
                .ThenBy(product => product.Name),
            ProductSort.NameAscending => projected.OrderBy(product => product.Name),
            _ => projected
                .OrderByDescending(product => product.IsPopular)
                .ThenByDescending(product => product.IsXtra)
                .ThenByDescending(product => product.PublishedAtUtc)
                .ThenBy(product => product.Name),
        };

        var totalItems = await projected.CountAsync(cancellationToken);
        var productIds = await projected
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);

        var loadedProducts = await LoadProducts(productIds, cancellationToken);
        var productsById = loadedProducts.ToDictionary(product => product.Id);
        var items = productIds
            .Where(productsById.ContainsKey)
            .Select(id => MapCard(productsById[id], now))
            .ToList();

        return new PagedProductResult(
            items,
            query.Page,
            query.PageSize,
            totalItems,
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)query.PageSize)
        );
    }

    public async Task<ProductDetails?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var product = await ProductDetailsQuery()
            .SingleOrDefaultAsync(
                item => item.Status == ProductStatus.Active && item.Slug == slug.Trim(),
                cancellationToken
            );

        return product is null ? null : MapDetails(product, DateTimeOffset.UtcNow);
    }

    private async Task<List<Product>> LoadProducts(
        List<Guid> productIds,
        CancellationToken cancellationToken
    )
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        return await ProductDetailsQuery()
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Product> ProductDetailsQuery()
    {
        return dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(product => product.ProductCategories)
                .ThenInclude(item => item.Category)
            .Include(product => product.Images)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Options)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Prices)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.CampaignPrices)
                    .ThenInclude(price => price.Campaign)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.Stocks);
    }

    private static ProductCard MapCard(Product product, DateTimeOffset now)
    {
        var activeVariants = product.Variants.Where(variant => variant.IsActive).ToList();
        var pricedVariants = activeVariants
            .Select(variant => new { Variant = variant, Price = MapPrice(variant, now) })
            .Where(item => item.Price is not null)
            .OrderBy(item => item.Price!.EffectivePrice)
            .ToList();
        var selectedVariant = pricedVariants.FirstOrDefault();

        return new ProductCard(
            product.Id,
            product.ProductCode,
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.IsPopular,
            product.IsXtra,
            product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .Select(MapImage)
                .FirstOrDefault(),
            selectedVariant?.Price,
            MapStock(activeVariants.SelectMany(variant => variant.Stocks)),
            MapCategories(product)
        );
    }

    private static ProductDetails MapDetails(Product product, DateTimeOffset now)
    {
        var variants = product.Variants
            .Where(variant => variant.IsActive)
            .OrderByDescending(variant => variant.IsDefault)
            .ThenBy(variant => variant.Name)
            .Select(variant => new ProductVariantSummary(
                variant.Id,
                variant.Name,
                variant.Sku,
                variant.Barcode,
                variant.IsDefault,
                MapPrice(variant, now),
                MapStock(variant.Stocks),
                variant.Options
                    .OrderBy(option => option.DisplayOrder)
                    .Select(option => new VariantOptionSummary(
                        option.OptionName,
                        option.OptionValue,
                        option.DisplayOrder
                    ))
                    .ToList()
            ))
            .ToList();

        return new ProductDetails(
            product.Id,
            product.ProductCode,
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.Description,
            product.IsPopular,
            product.IsXtra,
            product.PublishedAtUtc,
            MapCategories(product),
            product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .Select(MapImage)
                .ToList(),
            variants
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

    private static StockSummary MapStock(IEnumerable<Stock> stocks)
    {
        var availableQuantity = stocks.Sum(stock =>
            stock.OnHandQuantity - stock.ReservedQuantity);
        var status = availableQuantity switch
        {
            <= 0 => "OutOfStock",
            <= 3 => "LowStock",
            _ => "InStock",
        };

        return new StockSummary(availableQuantity, status);
    }

    private static ProductImageSummary MapImage(ProductImage image)
    {
        return new ProductImageSummary(
            image.Id,
            image.VariantId,
            image.ImageUrl,
            image.AltText,
            image.IsPrimary,
            image.DisplayOrder
        );
    }

    private static List<CategorySummary> MapCategories(Product product)
    {
        return product.ProductCategories
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
            .ToList();
    }

    private sealed class ProductListingProjection
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public bool IsPopular { get; init; }
        public bool IsXtra { get; init; }
        public DateTimeOffset? PublishedAtUtc { get; init; }
        public decimal? MinimumPrice { get; init; }
    }
}
