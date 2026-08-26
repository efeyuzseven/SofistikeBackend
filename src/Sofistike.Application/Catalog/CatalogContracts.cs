using Sofistike.Domain.Catalog;

namespace Sofistike.Application.Catalog;

public enum ProductSort
{
    Recommended,
    Newest,
    PriceAscending,
    PriceDescending,
    NameAscending,
}

public sealed record ProductListQuery(
    string? Category,
    string? Search,
    bool? IsPopular,
    bool? IsXtra,
    bool? InStock,
    decimal? MinimumPrice,
    decimal? MaximumPrice,
    ProductSort Sort,
    int Page,
    int PageSize
);

public sealed record CategorySummary(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ParentSlug,
    CategoryMenuGroup MenuGroup,
    int DisplayOrder
);

public sealed record ProductImageSummary(
    Guid Id,
    Guid? VariantId,
    string Url,
    string AltText,
    bool IsPrimary,
    int DisplayOrder
);

public sealed record ProductPriceSummary(
    decimal ListPrice,
    decimal? CampaignPrice,
    decimal EffectivePrice,
    string CurrencyCode,
    int? DiscountPercentage
);

public sealed record StockSummary(
    int AvailableQuantity,
    string Status
);

public sealed record ProductCard(
    Guid Id,
    string ProductCode,
    string Name,
    string Slug,
    string ShortDescription,
    bool IsPopular,
    bool IsXtra,
    ProductImageSummary? PrimaryImage,
    ProductPriceSummary? Price,
    StockSummary Stock,
    IReadOnlyList<CategorySummary> Categories
);

public sealed record VariantOptionSummary(
    string Name,
    string Value,
    int DisplayOrder
);

public sealed record ProductVariantSummary(
    Guid Id,
    string Name,
    string Sku,
    string? Barcode,
    bool IsDefault,
    ProductPriceSummary? Price,
    StockSummary Stock,
    IReadOnlyList<VariantOptionSummary> Options
);

public sealed record ProductDetails(
    Guid Id,
    string ProductCode,
    string Name,
    string Slug,
    string ShortDescription,
    string Description,
    bool IsPopular,
    bool IsXtra,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyList<CategorySummary> Categories,
    IReadOnlyList<ProductImageSummary> Images,
    IReadOnlyList<ProductVariantSummary> Variants
);

public sealed record PagedProductResult(
    IReadOnlyList<ProductCard> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);

public interface IProductCatalogService
{
    Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(
        CancellationToken cancellationToken = default
    );

    Task<PagedProductResult> GetProductsAsync(
        ProductListQuery query,
        CancellationToken cancellationToken = default
    );

    Task<ProductDetails?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default
    );
}

public sealed record CreateProductCommand(
    string ProductCode,
    string Name,
    string Slug,
    string ShortDescription,
    string Description,
    IReadOnlyList<Guid> CategoryIds,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    bool IsPopular,
    bool IsXtra
);

public enum CreateProductStatus
{
    Created,
    CategoryNotFound,
    DuplicateProductCode,
    DuplicateSlug,
}

public sealed record CreateProductResult(
    CreateProductStatus Status,
    ProductDetails? Product
);

public sealed record UpdateProductCommand(
    string ProductCode,
    string Name,
    string Slug,
    string ShortDescription,
    string Description,
    IReadOnlyList<Guid> CategoryIds,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    bool IsPopular,
    bool IsXtra
);

public enum UpdateProductStatus
{
    Updated,
    NotFound,
    CategoryNotFound,
    DefaultVariantNotFound,
    DuplicateProductCode,
    DuplicateSlug,
    ConcurrencyConflict,
}

public sealed record UpdateProductResult(
    UpdateProductStatus Status,
    ProductDetails? Product
);

public enum ArchiveProductStatus
{
    Archived,
    NotFound,
    ConcurrencyConflict,
}

public interface IProductManagementService
{
    Task<CreateProductResult> CreateAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default
    );

    Task<UpdateProductResult> UpdateAsync(
        Guid productId,
        UpdateProductCommand command,
        CancellationToken cancellationToken = default
    );

    Task<ArchiveProductStatus> ArchiveAsync(
        Guid productId,
        CancellationToken cancellationToken = default
    );
}
