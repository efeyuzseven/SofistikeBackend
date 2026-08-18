using Sofistike.Application.Catalog;

namespace Sofistike.Application.Favorites;

public sealed record FavoriteItem(
    DateTimeOffset AddedAtUtc,
    ProductCard Product
);

public sealed record PagedFavoriteResult(
    IReadOnlyList<FavoriteItem> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);

public enum AddFavoriteStatus
{
    Added,
    AlreadyExists,
    ProductNotFound,
    UserNotFound,
}

public sealed record AddFavoriteResult(
    AddFavoriteStatus Status,
    FavoriteItem? Favorite
);

public interface IFavoriteService
{
    Task<PagedFavoriteResult> GetAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    Task<AddFavoriteResult> AddAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default
    );

    Task RemoveAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default
    );
}
