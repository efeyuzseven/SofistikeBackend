using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Catalog;
using Sofistike.Domain.Catalog;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Catalog;

public sealed class ProductManagementService(
    SofistikeDbContext dbContext,
    IProductCatalogService productCatalogService
) : IProductManagementService
{
    public async Task<CreateProductResult> CreateAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var productCode = command.ProductCode.Trim().ToUpperInvariant();
        var slug = command.Slug.Trim().ToLowerInvariant();

        if (await dbContext.Products.AnyAsync(
            product => product.ProductCode == productCode,
            cancellationToken
        ))
        {
            return new CreateProductResult(
                CreateProductStatus.DuplicateProductCode,
                null
            );
        }

        if (await dbContext.Products.AnyAsync(
            product => product.Slug == slug,
            cancellationToken
        ))
        {
            return new CreateProductResult(CreateProductStatus.DuplicateSlug, null);
        }

        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == command.CategoryId && item.IsActive,
            cancellationToken
        );
        if (category is null)
        {
            return new CreateProductResult(
                CreateProductStatus.CategoryNotFound,
                null
            );
        }

        var warehouse = await dbContext.Warehouses.SingleOrDefaultAsync(
            item => item.Code == "MAIN",
            cancellationToken
        );
        if (warehouse is null)
        {
            warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Code = "MAIN",
                Name = "Ana Depo",
                IsActive = true,
            };
            dbContext.Warehouses.Add(warehouse);
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            ProductCode = productCode,
            Name = command.Name.Trim(),
            Slug = slug,
            ShortDescription = command.ShortDescription.Trim(),
            Description = command.Description.Trim(),
            Status = ProductStatus.Active,
            IsPopular = command.IsPopular,
            IsXtra = command.IsXtra,
            PublishedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Name = "Standart",
            Sku = $"{productCode}-STD",
            IsDefault = true,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        product.ProductCategories.Add(new ProductCategory
        {
            ProductId = product.Id,
            Product = product,
            CategoryId = category.Id,
            Category = category,
            IsPrimary = true,
        });
        product.Images.Add(new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            ImageUrl = string.IsNullOrWhiteSpace(command.ImageUrl)
                ? "/images/hero-home.png"
                : command.ImageUrl.Trim(),
            AltText = product.Name,
            IsPrimary = true,
            DisplayOrder = 1,
        });
        variant.Prices.Add(new ProductPrice
        {
            Id = Guid.NewGuid(),
            VariantId = variant.Id,
            Variant = variant,
            Amount = command.Price,
            CurrencyCode = "TRY",
            ValidFromUtc = now,
        });
        variant.Stocks.Add(new Stock
        {
            VariantId = variant.Id,
            Variant = variant,
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            OnHandQuantity = command.StockQuantity,
            ReservedQuantity = 0,
            UpdatedAtUtc = now,
        });
        product.Variants.Add(variant);
        dbContext.Products.Add(product);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await dbContext.Products.AnyAsync(
                item => item.ProductCode == productCode,
                cancellationToken
            ))
            {
                return new CreateProductResult(
                    CreateProductStatus.DuplicateProductCode,
                    null
                );
            }

            if (await dbContext.Products.AnyAsync(
                item => item.Slug == slug,
                cancellationToken
            ))
            {
                return new CreateProductResult(
                    CreateProductStatus.DuplicateSlug,
                    null
                );
            }

            throw;
        }

        var created = await productCatalogService.GetProductBySlugAsync(
            slug,
            cancellationToken
        );
        return new CreateProductResult(CreateProductStatus.Created, created);
    }

    public async Task<UpdateProductResult> UpdateAsync(
        Guid productId,
        UpdateProductCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await dbContext.Products
            .AsSplitQuery()
            .Include(item => item.ProductCategories)
            .Include(item => item.Images)
            .Include(item => item.Variants)
                .ThenInclude(variant => variant.Prices)
            .Include(item => item.Variants)
                .ThenInclude(variant => variant.Stocks)
            .SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null || product.Status == ProductStatus.Archived)
        {
            return new UpdateProductResult(UpdateProductStatus.NotFound, null);
        }

        var productCode = command.ProductCode.Trim().ToUpperInvariant();
        var slug = command.Slug.Trim().ToLowerInvariant();
        if (await dbContext.Products.AnyAsync(
            item => item.Id != productId && item.ProductCode == productCode,
            cancellationToken
        ))
        {
            return new UpdateProductResult(
                UpdateProductStatus.DuplicateProductCode,
                null
            );
        }

        if (await dbContext.Products.AnyAsync(
            item => item.Id != productId && item.Slug == slug,
            cancellationToken
        ))
        {
            return new UpdateProductResult(UpdateProductStatus.DuplicateSlug, null);
        }

        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == command.CategoryId && item.IsActive,
            cancellationToken
        );
        if (category is null)
        {
            return new UpdateProductResult(
                UpdateProductStatus.CategoryNotFound,
                null
            );
        }

        var variant = product.Variants
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.CreatedAtUtc)
            .FirstOrDefault();
        if (variant is null)
        {
            return new UpdateProductResult(
                UpdateProductStatus.DefaultVariantNotFound,
                null
            );
        }

        var warehouse = await GetOrCreateMainWarehouseAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        product.ProductCode = productCode;
        product.Name = command.Name.Trim();
        product.Slug = slug;
        product.ShortDescription = command.ShortDescription.Trim();
        product.Description = command.Description.Trim();
        product.Status = ProductStatus.Active;
        product.IsPopular = command.IsPopular;
        product.IsXtra = command.IsXtra;
        product.PublishedAtUtc ??= now;
        product.UpdatedAtUtc = now;
        variant.IsActive = true;
        variant.UpdatedAtUtc = now;

        var selectedCategory = product.ProductCategories.SingleOrDefault(item =>
            item.CategoryId == category.Id);
        dbContext.ProductCategories.RemoveRange(
            product.ProductCategories.Where(item => item.CategoryId != category.Id)
        );
        if (selectedCategory is null)
        {
            dbContext.ProductCategories.Add(new ProductCategory
            {
                ProductId = product.Id,
                Product = product,
                CategoryId = category.Id,
                Category = category,
                IsPrimary = true,
            });
        }
        else
        {
            selectedCategory.IsPrimary = true;
        }

        var image = product.Images
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.DisplayOrder)
            .FirstOrDefault(item => item.VariantId is null);
        var imageUrl = string.IsNullOrWhiteSpace(command.ImageUrl)
            ? "/images/hero-home.png"
            : command.ImageUrl.Trim();
        if (image is null)
        {
            dbContext.ProductImages.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Product = product,
                ImageUrl = imageUrl,
                AltText = product.Name,
                IsPrimary = true,
                DisplayOrder = 1,
            });
        }
        else
        {
            image.ImageUrl = imageUrl;
            image.AltText = product.Name;
            image.IsPrimary = true;
        }

        var currentPrices = variant.Prices
            .Where(price =>
                price.CurrencyCode == "TRY"
                && price.ValidFromUtc <= now
                && (price.ValidToUtc is null || price.ValidToUtc > now))
            .OrderByDescending(price => price.ValidFromUtc)
            .ToList();
        if (currentPrices.Count == 0 || currentPrices[0].Amount != command.Price)
        {
            foreach (var price in currentPrices)
            {
                price.ValidToUtc = now;
            }

            dbContext.ProductPrices.Add(new ProductPrice
            {
                Id = Guid.NewGuid(),
                VariantId = variant.Id,
                Variant = variant,
                Amount = command.Price,
                CurrencyCode = "TRY",
                ValidFromUtc = now,
            });
        }

        var stock = variant.Stocks.SingleOrDefault(item =>
            item.WarehouseId == warehouse.Id);
        if (stock is null)
        {
            dbContext.Stocks.Add(new Stock
            {
                VariantId = variant.Id,
                Variant = variant,
                WarehouseId = warehouse.Id,
                Warehouse = warehouse,
                OnHandQuantity = command.StockQuantity,
                ReservedQuantity = 0,
                UpdatedAtUtc = now,
            });
        }
        else
        {
            stock.OnHandQuantity = command.StockQuantity + stock.ReservedQuantity;
            stock.UpdatedAtUtc = now;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateProductResult(
                UpdateProductStatus.ConcurrencyConflict,
                null
            );
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (await dbContext.Products.AnyAsync(
                item => item.Id != productId && item.ProductCode == productCode,
                cancellationToken
            ))
            {
                return new UpdateProductResult(
                    UpdateProductStatus.DuplicateProductCode,
                    null
                );
            }

            if (await dbContext.Products.AnyAsync(
                item => item.Id != productId && item.Slug == slug,
                cancellationToken
            ))
            {
                return new UpdateProductResult(
                    UpdateProductStatus.DuplicateSlug,
                    null
                );
            }

            throw;
        }

        var updated = await productCatalogService.GetProductBySlugAsync(
            slug,
            cancellationToken
        );
        return new UpdateProductResult(UpdateProductStatus.Updated, updated);
    }

    public async Task<ArchiveProductStatus> ArchiveAsync(
        Guid productId,
        CancellationToken cancellationToken = default
    )
    {
        var product = await dbContext.Products
            .Include(item => item.Variants)
            .SingleOrDefaultAsync(item => item.Id == productId, cancellationToken);
        if (product is null)
        {
            return ArchiveProductStatus.NotFound;
        }

        if (product.Status == ProductStatus.Archived)
        {
            return ArchiveProductStatus.Archived;
        }

        var now = DateTimeOffset.UtcNow;
        product.Status = ProductStatus.Archived;
        product.IsPopular = false;
        product.IsXtra = false;
        product.UpdatedAtUtc = now;
        foreach (var variant in product.Variants)
        {
            variant.IsActive = false;
            variant.UpdatedAtUtc = now;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return ArchiveProductStatus.Archived;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ArchiveProductStatus.ConcurrencyConflict;
        }
    }

    private async Task<Warehouse> GetOrCreateMainWarehouseAsync(
        CancellationToken cancellationToken
    )
    {
        var warehouse = await dbContext.Warehouses.SingleOrDefaultAsync(
            item => item.Code == "MAIN",
            cancellationToken
        );
        if (warehouse is not null)
        {
            return warehouse;
        }

        warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Code = "MAIN",
            Name = "Ana Depo",
            IsActive = true,
        };
        dbContext.Warehouses.Add(warehouse);
        return warehouse;
    }
}
