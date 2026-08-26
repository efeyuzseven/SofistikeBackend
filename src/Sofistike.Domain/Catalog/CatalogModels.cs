namespace Sofistike.Domain.Catalog;

public enum ProductStatus
{
    Draft,
    Active,
    Passive,
    Archived,
}

public enum CategoryMenuGroup
{
    Category,
    Solution,
    Room,
}

public sealed class Category
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public CategoryMenuGroup MenuGroup { get; set; } = CategoryMenuGroup.Category;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<ProductCategory> ProductCategories { get; set; } = [];
}

public sealed class Product
{
    public Guid Id { get; set; }
    public required string ProductCode { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string ShortDescription { get; set; }
    public required string Description { get; set; }
    public ProductStatus Status { get; set; }
    public bool IsPopular { get; set; }
    public bool IsXtra { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<ProductCategory> ProductCategories { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductVariant> Variants { get; set; } = [];
}

public sealed class ProductCategory
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public bool IsPrimary { get; set; }
}

public sealed class ProductVariant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public required string Name { get; set; }
    public required string Sku { get; set; }
    public string? Barcode { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<ProductVariantOption> Options { get; set; } = [];
    public ICollection<ProductPrice> Prices { get; set; } = [];
    public ICollection<CampaignPrice> CampaignPrices { get; set; } = [];
    public ICollection<Stock> Stocks { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
}

public sealed class ProductVariantOption
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public required string OptionName { get; set; }
    public required string OptionValue { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
    public required string ImageUrl { get; set; }
    public required string AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class ProductPrice
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public decimal Amount { get; set; }
    public required string CurrencyCode { get; set; }
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset? ValidToUtc { get; set; }
}

public sealed class Campaign
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public ICollection<CampaignPrice> Prices { get; set; } = [];
}

public sealed class CampaignPrice
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public decimal DiscountedAmount { get; set; }
}

public sealed class Warehouse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Stock> Stocks { get; set; } = [];
}

public sealed class Stock
{
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int OnHandQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
