using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Catalog;
using Sofistike.Domain.Catalog;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Catalog;

public sealed class CategoryManagementService(SofistikeDbContext dbContext)
    : ICategoryManagementService
{
    public async Task<IReadOnlyList<ManagedCategoryDetails>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.MenuGroup)
            .ThenBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new ManagedCategoryDetails(
                category.Id,
                category.Name,
                category.Slug,
                category.Description,
                category.MenuGroup,
                category.DisplayOrder,
                category.IsActive,
                category.ProductCategories.Count,
                category.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<SaveCategoryResult> CreateAsync(
        SaveCategoryCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var slug = NormalizeSlug(command.Slug);
        if (await SlugExistsAsync(slug, null, cancellationToken))
        {
            return new SaveCategoryResult(SaveCategoryStatus.DuplicateSlug, null);
        }

        var now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Slug = slug,
            Description = Normalize(command.Description),
            MenuGroup = command.MenuGroup,
            DisplayOrder = command.DisplayOrder,
            IsActive = command.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        dbContext.Categories.Add(category);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await SlugExistsAsync(slug, null, cancellationToken))
            {
                return new SaveCategoryResult(
                    SaveCategoryStatus.DuplicateSlug,
                    null
                );
            }

            throw;
        }

        return new SaveCategoryResult(
            SaveCategoryStatus.Saved,
            Map(category, 0)
        );
    }

    public async Task<SaveCategoryResult> UpdateAsync(
        Guid categoryId,
        SaveCategoryCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == categoryId,
            cancellationToken
        );
        if (category is null)
        {
            return new SaveCategoryResult(SaveCategoryStatus.NotFound, null);
        }

        var slug = NormalizeSlug(command.Slug);
        if (await SlugExistsAsync(slug, categoryId, cancellationToken))
        {
            return new SaveCategoryResult(SaveCategoryStatus.DuplicateSlug, null);
        }

        category.Name = command.Name.Trim();
        category.Slug = slug;
        category.Description = Normalize(command.Description);
        category.MenuGroup = command.MenuGroup;
        category.DisplayOrder = command.DisplayOrder;
        category.IsActive = command.IsActive;
        category.UpdatedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (await SlugExistsAsync(slug, categoryId, cancellationToken))
            {
                return new SaveCategoryResult(
                    SaveCategoryStatus.DuplicateSlug,
                    null
                );
            }

            throw;
        }

        var productCount = await dbContext.ProductCategories.CountAsync(
            item => item.CategoryId == categoryId,
            cancellationToken
        );
        return new SaveCategoryResult(
            SaveCategoryStatus.Saved,
            Map(category, productCount)
        );
    }

    public async Task<DeleteCategoryStatus> DeleteAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    )
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == categoryId,
            cancellationToken
        );
        if (category is null)
        {
            return DeleteCategoryStatus.NotFound;
        }

        var isInUse = await dbContext.ProductCategories.AnyAsync(
            item => item.CategoryId == categoryId,
            cancellationToken
        ) || await dbContext.Categories.AnyAsync(
            item => item.ParentCategoryId == categoryId,
            cancellationToken
        );
        if (isInUse)
        {
            return DeleteCategoryStatus.InUse;
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return DeleteCategoryStatus.Deleted;
    }

    private Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludedCategoryId,
        CancellationToken cancellationToken
    ) => dbContext.Categories.AnyAsync(
        category =>
            category.Slug == slug
            && (!excludedCategoryId.HasValue || category.Id != excludedCategoryId),
        cancellationToken
    );

    private static string NormalizeSlug(string value) =>
        value.Trim().ToLowerInvariant();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ManagedCategoryDetails Map(Category category, int productCount) =>
        new(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.MenuGroup,
            category.DisplayOrder,
            category.IsActive,
            productCount,
            category.UpdatedAtUtc
        );
}
