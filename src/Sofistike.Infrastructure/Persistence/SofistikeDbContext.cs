using Microsoft.EntityFrameworkCore;
using Sofistike.Domain.Catalog;
using Sofistike.Domain.Favorites;
using Sofistike.Domain.Sales;
using Sofistike.Domain.Users;

namespace Sofistike.Infrastructure.Persistence;

public sealed class SofistikeDbContext(DbContextOptions<SofistikeDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantOption> ProductVariantOptions => Set<ProductVariantOption>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignPrice> CampaignPrices => Set<CampaignPrice>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<UserFavorite> UserFavorites => Set<UserFavorite>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SofistikeDbContext).Assembly
        );
    }
}
