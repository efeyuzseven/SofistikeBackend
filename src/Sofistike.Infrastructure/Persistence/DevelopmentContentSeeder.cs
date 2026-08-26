using Microsoft.EntityFrameworkCore;
using Sofistike.Domain.Content;

namespace Sofistike.Infrastructure.Persistence;

public static class DevelopmentContentSeeder
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 8, 26, 0, 0, 0, TimeSpan.Zero);

    public static async Task SeedAsync(
        SofistikeDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (await dbContext.HomeBanners.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.HomeBanners.AddRange(
            CreateBanner(
                "a318222d-df2c-4f69-9f0b-f158268f8a01",
                "/images/sofistike-manifesto-hero.webp",
                "Sofistike +XTRA - Smart Ideas. Better Living marka manifestosu",
                1
            ),
            CreateBanner(
                "a318222d-df2c-4f69-9f0b-f158268f8a02",
                "/images/hero-living.png",
                "Sofistike +XTRA ev yaşam ürünleri koleksiyonu",
                2
            ),
            CreateBanner(
                "a318222d-df2c-4f69-9f0b-f158268f8a03",
                "/images/hero-home.png",
                "Sofistike +XTRA aroma ve ev tekstili ürünleri",
                3
            ),
            CreateBanner(
                "a318222d-df2c-4f69-9f0b-f158268f8a04",
                "/images/hero-sleep.png",
                "Sofistike +XTRA uyku çözümleri",
                4
            )
        );

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static HomeBanner CreateBanner(
        string id,
        string imageUrl,
        string altText,
        int displayOrder
    ) => new()
    {
        Id = Guid.Parse(id),
        ImageUrl = imageUrl,
        AltText = altText,
        DisplayOrder = displayOrder,
        IsActive = true,
        CreatedAtUtc = SeededAt,
        UpdatedAtUtc = SeededAt,
    };
}
