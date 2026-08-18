using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofistike.Domain.Sales;

namespace Sofistike.Infrastructure.Persistence.Configurations;

public sealed class ShoppingCartConfiguration
    : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ShoppingCarts", "sales");
        builder.HasKey(cart => cart.Id);
        builder.Property(cart => cart.CreatedAtUtc).HasPrecision(0);
        builder.Property(cart => cart.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(cart => cart.UserId).IsUnique();
        builder.HasOne(cart => cart.User)
            .WithMany()
            .HasForeignKey(cart => cart.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CartItems", "sales", table =>
            table.HasCheckConstraint("CK_CartItems_Quantity", "[Quantity] BETWEEN 1 AND 9"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.AddedAtUtc).HasPrecision(0);
        builder.Property(item => item.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(item => new { item.CartId, item.ProductVariantId }).IsUnique();
        builder.HasOne(item => item.Cart)
            .WithMany(cart => cart.Items)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.ProductVariant)
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Orders", "sales", table =>
        {
            table.HasCheckConstraint("CK_Orders_Subtotal", "[Subtotal] >= 0");
            table.HasCheckConstraint("CK_Orders_ShippingAmount", "[ShippingAmount] >= 0");
            table.HasCheckConstraint("CK_Orders_TotalAmount", "[TotalAmount] >= 0");
        });
        builder.HasKey(order => order.Id);
        builder.Property(order => order.OrderNumber).HasMaxLength(40).IsRequired();
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(order => order.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(order => order.DeliveryMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(order => order.ContactEmail).HasMaxLength(320).IsRequired();
        builder.Property(order => order.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(order => order.LastName).HasMaxLength(100).IsRequired();
        builder.Property(order => order.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(order => order.City).HasMaxLength(100).IsRequired();
        builder.Property(order => order.District).HasMaxLength(120).IsRequired();
        builder.Property(order => order.AddressLine).HasMaxLength(1000).IsRequired();
        builder.Property(order => order.PostalCode).HasMaxLength(20);
        builder.Property(order => order.AddressTitle).HasMaxLength(100);
        builder.Property(order => order.Subtotal).HasPrecision(18, 2);
        builder.Property(order => order.ShippingAmount).HasPrecision(18, 2);
        builder.Property(order => order.TotalAmount).HasPrecision(18, 2);
        builder.Property(order => order.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(order => order.CreatedAtUtc).HasPrecision(0);
        builder.Property(order => order.UpdatedAtUtc).HasPrecision(0);
        builder.HasIndex(order => order.OrderNumber).IsUnique();
        builder.HasIndex(order => new { order.UserId, order.CreatedAtUtc });
        builder.HasOne(order => order.User)
            .WithMany()
            .HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OrderItems", "sales", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_OrderItems_UnitPrice", "[UnitPrice] >= 0");
            table.HasCheckConstraint("CK_OrderItems_LineTotal", "[LineTotal] >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductName).HasMaxLength(250).IsRequired();
        builder.Property(item => item.VariantName).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Sku).HasMaxLength(100).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.LineTotal).HasPrecision(18, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.HasIndex(item => item.OrderId);
        builder.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
