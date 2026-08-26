using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofistike.Domain.Content;

namespace Sofistike.Infrastructure.Persistence.Configurations;

public sealed class HomeBannerConfiguration : IEntityTypeConfiguration<HomeBanner>
{
    public void Configure(EntityTypeBuilder<HomeBanner> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("HomeBanners", "content", table =>
            table.HasCheckConstraint(
                "CK_HomeBanners_DisplayOrder",
                "[DisplayOrder] >= 0"
            )
        );
        builder.HasKey(banner => banner.Id);
        builder.Property(banner => banner.ImageUrl).HasMaxLength(2000).IsRequired();
        builder.Property(banner => banner.AltText).HasMaxLength(300).IsRequired();
        builder.Property(banner => banner.Title).HasMaxLength(200);
        builder.Property(banner => banner.Description).HasMaxLength(750);
        builder.Property(banner => banner.ButtonText).HasMaxLength(100);
        builder.Property(banner => banner.LinkUrl).HasMaxLength(2000);
        builder.Property(banner => banner.CreatedAtUtc).HasPrecision(0);
        builder.Property(banner => banner.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(banner => new { banner.IsActive, banner.DisplayOrder });
    }
}
