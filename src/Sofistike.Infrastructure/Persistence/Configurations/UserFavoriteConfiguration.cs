using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofistike.Domain.Favorites;

namespace Sofistike.Infrastructure.Persistence.Configurations;

public sealed class UserFavoriteConfiguration
    : IEntityTypeConfiguration<UserFavorite>
{
    public void Configure(EntityTypeBuilder<UserFavorite> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UserFavorites", "customer");
        builder.HasKey(favorite => new { favorite.UserId, favorite.ProductId });
        builder.Property(favorite => favorite.CreatedAtUtc).HasPrecision(0);
        builder.HasIndex(favorite => new { favorite.UserId, favorite.CreatedAtUtc });
        builder.HasOne(favorite => favorite.User)
            .WithMany()
            .HasForeignKey(favorite => favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(favorite => favorite.Product)
            .WithMany()
            .HasForeignKey(favorite => favorite.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
