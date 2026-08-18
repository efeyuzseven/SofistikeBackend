using Microsoft.EntityFrameworkCore;
using Sofistike.Domain.Catalog;

namespace Sofistike.Infrastructure.Persistence;

public static class DevelopmentCatalogSeeder
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

    public static async Task SeedAsync(
        SofistikeDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = CreateCategories();
        var warehouse = new Warehouse
        {
            Id = Guid.Parse("d53ca179-62b7-4b1d-af25-f2909e54fae7"),
            Code = "MAIN",
            Name = "Ana Depo",
            IsActive = true,
        };
        var campaign = new Campaign
        {
            Id = Guid.Parse("84490354-f9c8-4937-a2d5-d1af3f653d29"),
            Name = "+XTRA Tanışma Fiyatları",
            Slug = "xtra-tanisma-fiyatlari",
            IsActive = true,
            StartsAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndsAtUtc = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CreatedAtUtc = SeededAt,
        };

        dbContext.Categories.AddRange(categories.Values);
        dbContext.Warehouses.Add(warehouse);
        dbContext.Campaigns.Add(campaign);

        var definitions = new[]
        {
            new SeedProduct(
                "05527362-1d91-4b47-a598-bf334c4996bb",
                "XTRA-AROMA-001",
                "+XTRA Sakin Aroma",
                "xtra-sakin-aroma",
                "Lavanta ve amber notalarıyla evin havasını yumuşatır.",
                "Kullanıcı içgörüleriyle geliştirilen dengeli koku yoğunluğuna sahip ev aroması.",
                "aroma",
                "/images/hero-home.png",
                399m,
                349m,
                18,
                true,
                true
            ),
            new SeedProduct(
                "3310ead5-3459-43a7-982f-6446cc5af664",
                "XTRA-SLEEP-001",
                "+XTRA One Konfor Yastığı",
                "xtra-one-konfor-yastigi",
                "Ayarlanabilir dolgu ile kişiselleştirilen uyku konforu.",
                "Uyku alışkanlıklarına göre dolgu miktarı ayarlanabilen +XTRA konfor yastığı.",
                "uyku",
                "/images/hero-sleep.png",
                999m,
                null,
                12,
                true,
                true
            ),
            new SeedProduct(
                "8529ea32-50b0-476f-aeb0-8656aa5b0d3f",
                "TEXTILE-001",
                "Lavanta Tekstil Spreyi",
                "lavanta-tekstil-spreyi",
                "Kullanıcı yorumlarıyla geliştirilen uzun süreli ferahlık.",
                "Ev tekstillerinde kalıcı ve dengeli lavanta ferahlığı sağlayan bakım spreyi.",
                "ev-tekstili",
                "/images/hero-home.png",
                279m,
                null,
                24,
                false,
                false
            ),
            new SeedProduct(
                "cf6d1497-6c1f-4997-84ae-713410eed466",
                "KITCHEN-001",
                "Limon Bulaşık Deterjanı",
                "limon-bulasik-deterjani",
                "Kolay durulanan formül ve canlı limon ferahlığı.",
                "Günlük bulaşık temizliğinde etkili, kolay durulanan limon kokulu deterjan.",
                "mutfak",
                "/images/hero-home.png",
                189m,
                null,
                30,
                true,
                false
            ),
            new SeedProduct(
                "6170d843-e8b7-4a6e-943f-d0c5fc6b69c3",
                "BATH-001",
                "Yumuşak Dokulu Havlu Seti",
                "yumusak-dokulu-havlu-seti",
                "Emicilik ve dokunma hissi kullanıcı notlarıyla yenilendi.",
                "Banyo kullanımına uygun, yüksek emicilik sunan yumuşak dokulu havlu seti.",
                "banyo",
                "/images/hero-living.png",
                699m,
                null,
                3,
                false,
                false
            ),
            new SeedProduct(
                "0439679f-f073-4663-81d8-b96a395e40ab",
                "PET-001",
                "Evcil Dostlar Bakım Seti",
                "evcil-dostlar-bakim-seti",
                "Günlük bakım için sade, güvenli ve pratik çözümler.",
                "Evcil dostların günlük bakım ihtiyaçlarını bir araya getiren pratik set.",
                "evcil-dostlar",
                "/images/module-sprite.png",
                429m,
                null,
                0,
                false,
                false
            ),
        };

        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index];
            var product = CreateProduct(
                definition,
                categories[definition.CategorySlug],
                warehouse,
                campaign,
                index
            );
            dbContext.Products.Add(product);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, Category> CreateCategories()
    {
        var definitions = new[]
        {
            ("a8dbe6dd-d2d3-4e8a-abab-fe73a4ed51e0", "Uyku", "uyku"),
            ("b521da36-abbe-408b-b191-2cb24c2bb7b5", "Ev Tekstili", "ev-tekstili"),
            ("552528a6-d27c-4419-b007-d2a5e30b934b", "Aroma", "aroma"),
            ("f7487f1f-f29a-487e-8ed2-d0c15d9f6681", "Mutfak", "mutfak"),
            ("b67cd88e-5b72-42d9-b942-7f817383291c", "Banyo", "banyo"),
            ("35d2e8e7-8b03-4dbf-b808-b640ee3120bc", "Evcil Dostlar", "evcil-dostlar"),
        };

        return definitions
            .Select((item, index) => new Category
            {
                Id = Guid.Parse(item.Item1),
                Name = item.Item2,
                Slug = item.Item3,
                Description = $"{item.Item2} kategorisindeki Sofistike ürünleri.",
                DisplayOrder = index + 1,
                IsActive = true,
                CreatedAtUtc = SeededAt,
                UpdatedAtUtc = SeededAt,
            })
            .ToDictionary(category => category.Slug);
    }

    private static Product CreateProduct(
        SeedProduct definition,
        Category category,
        Warehouse warehouse,
        Campaign campaign,
        int index
    )
    {
        var productId = Guid.Parse(definition.Id);
        var variantId = CreateStableGuid(productId, 1);
        var product = new Product
        {
            Id = productId,
            ProductCode = definition.Code,
            Name = definition.Name,
            Slug = definition.Slug,
            ShortDescription = definition.ShortDescription,
            Description = definition.Description,
            Status = ProductStatus.Active,
            IsPopular = definition.IsPopular,
            IsXtra = definition.IsXtra,
            PublishedAtUtc = SeededAt.AddDays(-index),
            CreatedAtUtc = SeededAt,
            UpdatedAtUtc = SeededAt,
        };
        var variant = new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Product = product,
            Name = "Standart",
            Sku = $"{definition.Code}-STD",
            IsDefault = true,
            IsActive = true,
            CreatedAtUtc = SeededAt,
            UpdatedAtUtc = SeededAt,
        };

        product.ProductCategories.Add(new ProductCategory
        {
            ProductId = productId,
            Product = product,
            CategoryId = category.Id,
            Category = category,
            IsPrimary = true,
        });
        product.Images.Add(new ProductImage
        {
            Id = CreateStableGuid(productId, 2),
            ProductId = productId,
            Product = product,
            ImageUrl = definition.ImageUrl,
            AltText = definition.Name,
            IsPrimary = true,
            DisplayOrder = 1,
        });
        variant.Prices.Add(new ProductPrice
        {
            Id = CreateStableGuid(productId, 3),
            VariantId = variantId,
            Variant = variant,
            Amount = definition.ListPrice,
            CurrencyCode = "TRY",
            ValidFromUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
        variant.Stocks.Add(new Stock
        {
            VariantId = variantId,
            Variant = variant,
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            OnHandQuantity = definition.StockQuantity,
            ReservedQuantity = 0,
            UpdatedAtUtc = SeededAt,
        });

        if (definition.CampaignPrice.HasValue)
        {
            variant.CampaignPrices.Add(new CampaignPrice
            {
                Id = CreateStableGuid(productId, 4),
                CampaignId = campaign.Id,
                Campaign = campaign,
                VariantId = variantId,
                Variant = variant,
                DiscountedAmount = definition.CampaignPrice.Value,
            });
        }

        product.Variants.Add(variant);
        return product;
    }

    private static Guid CreateStableGuid(Guid source, byte suffix)
    {
        var bytes = source.ToByteArray();
        bytes[^1] ^= suffix;
        return new Guid(bytes);
    }

    private sealed record SeedProduct(
        string Id,
        string Code,
        string Name,
        string Slug,
        string ShortDescription,
        string Description,
        string CategorySlug,
        string ImageUrl,
        decimal ListPrice,
        decimal? CampaignPrice,
        int StockQuantity,
        bool IsPopular,
        bool IsXtra
    );
}
