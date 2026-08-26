namespace Sofistike.Application.Content;

public sealed record HomeBannerDetails(
    Guid Id,
    string ImageUrl,
    string AltText,
    string? Title,
    string? Description,
    string? ButtonText,
    string? LinkUrl,
    int DisplayOrder,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc
);

public sealed record SaveHomeBannerCommand(
    string ImageUrl,
    string AltText,
    string? Title,
    string? Description,
    string? ButtonText,
    string? LinkUrl,
    int DisplayOrder,
    bool IsActive
);

public interface IHomeBannerService
{
    Task<IReadOnlyList<HomeBannerDetails>> GetActiveAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<HomeBannerDetails>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    Task<HomeBannerDetails> CreateAsync(
        SaveHomeBannerCommand command,
        CancellationToken cancellationToken = default
    );

    Task<HomeBannerDetails?> UpdateAsync(
        Guid bannerId,
        SaveHomeBannerCommand command,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(
        Guid bannerId,
        CancellationToken cancellationToken = default
    );
}
