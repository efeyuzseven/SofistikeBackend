using Sofistike.Domain.Catalog;

namespace Sofistike.Application.Catalog;

public sealed record ManagedCategoryDetails(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    CategoryMenuGroup MenuGroup,
    int DisplayOrder,
    bool IsActive,
    int ProductCount,
    DateTimeOffset UpdatedAtUtc
);

public sealed record SaveCategoryCommand(
    string Name,
    string Slug,
    string? Description,
    CategoryMenuGroup MenuGroup,
    int DisplayOrder,
    bool IsActive
);

public enum SaveCategoryStatus
{
    Saved,
    NotFound,
    DuplicateSlug,
}

public sealed record SaveCategoryResult(
    SaveCategoryStatus Status,
    ManagedCategoryDetails? Category
);

public enum DeleteCategoryStatus
{
    Deleted,
    NotFound,
    InUse,
}

public interface ICategoryManagementService
{
    Task<IReadOnlyList<ManagedCategoryDetails>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task<SaveCategoryResult> CreateAsync(
        SaveCategoryCommand command,
        CancellationToken cancellationToken = default
    );

    Task<SaveCategoryResult> UpdateAsync(
        Guid categoryId,
        SaveCategoryCommand command,
        CancellationToken cancellationToken = default
    );

    Task<DeleteCategoryStatus> DeleteAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    );
}
