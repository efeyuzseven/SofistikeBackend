using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofistike.Domain.Catalog;

namespace Sofistike.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Categories", "catalog", table =>
            table.HasCheckConstraint(
                "CK_Categories_MenuGroup",
                "[MenuGroup] IN ('Solution', 'Room', 'Category')"
            )
        );
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(150).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(180).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(1000);
        builder.Property(category => category.MenuGroup)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(CategoryMenuGroup.Category)
            .IsRequired();
        builder.Property(category => category.CreatedAtUtc).HasPrecision(0);
        builder.Property(category => category.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(category => category.Slug).IsUnique();
        builder.HasIndex(category => new
        {
            category.IsActive,
            category.MenuGroup,
            category.DisplayOrder,
        });
        builder.HasOne(category => category.ParentCategory)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Products", "catalog");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(250).IsRequired();
        builder.Property(product => product.Slug).HasMaxLength(280).IsRequired();
        builder.Property(product => product.ShortDescription).HasMaxLength(500).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(5000).IsRequired();
        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(product => product.PublishedAtUtc).HasPrecision(0);
        builder.Property(product => product.CreatedAtUtc).HasPrecision(0);
        builder.Property(product => product.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(product => product.ProductCode).IsUnique();
        builder.HasIndex(product => product.Slug).IsUnique();
        builder.HasIndex(product => new { product.Status, product.IsPopular, product.IsXtra });
    }
}

public sealed class ProductCategoryConfiguration
    : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProductCategories", "catalog");
        builder.HasKey(item => new { item.ProductId, item.CategoryId });
        builder.HasOne(item => item.Product)
            .WithMany(product => product.ProductCategories)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Category)
            .WithMany(category => category.ProductCategories)
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.CategoryId, item.IsPrimary });
    }
}

public sealed class ProductVariantConfiguration
    : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProductVariants", "catalog");
        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.Name).HasMaxLength(150).IsRequired();
        builder.Property(variant => variant.Sku).HasMaxLength(100).IsRequired();
        builder.Property(variant => variant.Barcode).HasMaxLength(50);
        builder.Property(variant => variant.CreatedAtUtc).HasPrecision(0);
        builder.Property(variant => variant.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(variant => variant.Sku).IsUnique();
        builder.HasIndex(variant => variant.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL");
        builder.HasIndex(variant => new { variant.ProductId, variant.IsActive, variant.IsDefault });
        builder.HasOne(variant => variant.Product)
            .WithMany(product => product.Variants)
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductVariantOptionConfiguration
    : IEntityTypeConfiguration<ProductVariantOption>
{
    public void Configure(EntityTypeBuilder<ProductVariantOption> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProductVariantOptions", "catalog");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.OptionName).HasMaxLength(100).IsRequired();
        builder.Property(option => option.OptionValue).HasMaxLength(150).IsRequired();
        builder.HasIndex(option => new { option.VariantId, option.OptionName }).IsUnique();
        builder.HasOne(option => option.Variant)
            .WithMany(variant => variant.Options)
            .HasForeignKey(option => option.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductImageConfiguration
    : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProductImages", "catalog");
        builder.HasKey(image => image.Id);
        builder.Property(image => image.ImageUrl).HasMaxLength(2000).IsRequired();
        builder.Property(image => image.AltText).HasMaxLength(300).IsRequired();
        builder.HasIndex(image => new { image.ProductId, image.DisplayOrder });
        builder.HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(image => image.Variant)
            .WithMany(variant => variant.Images)
            .HasForeignKey(image => image.VariantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ProductPriceConfiguration
    : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProductPrices", "catalog", table =>
            table.HasCheckConstraint("CK_ProductPrices_Amount", "[Amount] >= 0"));
        builder.HasKey(price => price.Id);
        builder.Property(price => price.Amount).HasPrecision(18, 2);
        builder.Property(price => price.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(price => price.ValidFromUtc).HasPrecision(0);
        builder.Property(price => price.ValidToUtc).HasPrecision(0);
        builder.HasIndex(price => new { price.VariantId, price.CurrencyCode, price.ValidFromUtc });
        builder.HasOne(price => price.Variant)
            .WithMany(variant => variant.Prices)
            .HasForeignKey(price => price.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Campaigns", "catalog", table =>
            table.HasCheckConstraint("CK_Campaigns_DateRange", "[EndsAtUtc] > [StartsAtUtc]"));
        builder.HasKey(campaign => campaign.Id);
        builder.Property(campaign => campaign.Name).HasMaxLength(200).IsRequired();
        builder.Property(campaign => campaign.Slug).HasMaxLength(220).IsRequired();
        builder.Property(campaign => campaign.StartsAtUtc).HasPrecision(0);
        builder.Property(campaign => campaign.EndsAtUtc).HasPrecision(0);
        builder.Property(campaign => campaign.CreatedAtUtc).HasPrecision(0);
        builder.HasIndex(campaign => campaign.Slug).IsUnique();
        builder.HasIndex(campaign => new { campaign.IsActive, campaign.StartsAtUtc, campaign.EndsAtUtc });
    }
}

public sealed class CampaignPriceConfiguration
    : IEntityTypeConfiguration<CampaignPrice>
{
    public void Configure(EntityTypeBuilder<CampaignPrice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CampaignPrices", "catalog", table =>
            table.HasCheckConstraint("CK_CampaignPrices_Amount", "[DiscountedAmount] >= 0"));
        builder.HasKey(price => price.Id);
        builder.Property(price => price.DiscountedAmount).HasPrecision(18, 2);
        builder.HasIndex(price => new { price.CampaignId, price.VariantId }).IsUnique();
        builder.HasOne(price => price.Campaign)
            .WithMany(campaign => campaign.Prices)
            .HasForeignKey(price => price.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(price => price.Variant)
            .WithMany(variant => variant.CampaignPrices)
            .HasForeignKey(price => price.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Warehouses", "catalog");
        builder.HasKey(warehouse => warehouse.Id);
        builder.Property(warehouse => warehouse.Code).HasMaxLength(50).IsRequired();
        builder.Property(warehouse => warehouse.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(warehouse => warehouse.Code).IsUnique();
    }
}

public sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Stocks", "catalog", table =>
        {
            table.HasCheckConstraint("CK_Stocks_OnHand", "[OnHandQuantity] >= 0");
            table.HasCheckConstraint("CK_Stocks_Reserved", "[ReservedQuantity] >= 0 AND [ReservedQuantity] <= [OnHandQuantity]");
        });
        builder.HasKey(stock => new { stock.VariantId, stock.WarehouseId });
        builder.Property(stock => stock.RowVersion).IsRowVersion();
        builder.Property(stock => stock.UpdatedAtUtc).HasPrecision(0);
        builder.HasOne(stock => stock.Variant)
            .WithMany(variant => variant.Stocks)
            .HasForeignKey(stock => stock.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(stock => stock.Warehouse)
            .WithMany(warehouse => warehouse.Stocks)
            .HasForeignKey(stock => stock.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
