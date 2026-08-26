using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofistike.Domain.Reviews;

namespace Sofistike.Infrastructure.Persistence.Configurations;

public sealed class ProductReviewConfiguration
    : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Reviews", "feedback", table =>
            table.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));
        builder.HasKey(review => review.Id);
        builder.Property(review => review.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(review => review.Comment).HasMaxLength(2000).IsRequired();
        builder.Property(review => review.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(review => review.CreatedAtUtc).HasPrecision(0);
        builder.Property(review => review.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(review => new { review.UserId, review.CreatedAtUtc });
        builder.HasIndex(review => new
        {
            review.UserId,
            review.OrderId,
            review.ProductId,
            review.Type,
        })
            .IsUnique();
        builder.HasOne<Sofistike.Domain.Users.UserAccount>()
            .WithMany()
            .HasForeignKey(review => review.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sofistike.Domain.Sales.Order>()
            .WithMany()
            .HasForeignKey(review => review.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sofistike.Domain.Catalog.Product>()
            .WithMany()
            .HasForeignKey(review => review.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
