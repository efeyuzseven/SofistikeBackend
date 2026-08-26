using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Content;
using Sofistike.Domain.Content;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Content;

public sealed class HomeBannerService(SofistikeDbContext dbContext)
    : IHomeBannerService
{
    public async Task<IReadOnlyList<HomeBannerDetails>> GetActiveAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await Query()
            .Where(banner => banner.IsActive)
            .OrderBy(banner => banner.DisplayOrder)
            .ThenBy(banner => banner.CreatedAtUtc)
            .Select(banner => new HomeBannerDetails(
                banner.Id,
                banner.ImageUrl,
                banner.AltText,
                banner.Title,
                banner.Description,
                banner.ButtonText,
                banner.LinkUrl,
                banner.DisplayOrder,
                banner.IsActive,
                banner.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HomeBannerDetails>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await Query()
            .OrderBy(banner => banner.DisplayOrder)
            .ThenBy(banner => banner.CreatedAtUtc)
            .Select(banner => new HomeBannerDetails(
                banner.Id,
                banner.ImageUrl,
                banner.AltText,
                banner.Title,
                banner.Description,
                banner.ButtonText,
                banner.LinkUrl,
                banner.DisplayOrder,
                banner.IsActive,
                banner.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<HomeBannerDetails> CreateAsync(
        SaveHomeBannerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = DateTimeOffset.UtcNow;
        var banner = new HomeBanner
        {
            Id = Guid.NewGuid(),
            ImageUrl = command.ImageUrl.Trim(),
            AltText = command.AltText.Trim(),
            Title = Normalize(command.Title),
            Description = Normalize(command.Description),
            ButtonText = Normalize(command.ButtonText),
            LinkUrl = Normalize(command.LinkUrl),
            DisplayOrder = command.DisplayOrder,
            IsActive = command.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        dbContext.HomeBanners.Add(banner);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(banner);
    }

    public async Task<HomeBannerDetails?> UpdateAsync(
        Guid bannerId,
        SaveHomeBannerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var banner = await dbContext.HomeBanners.SingleOrDefaultAsync(
            item => item.Id == bannerId,
            cancellationToken
        );
        if (banner is null)
        {
            return null;
        }

        banner.ImageUrl = command.ImageUrl.Trim();
        banner.AltText = command.AltText.Trim();
        banner.Title = Normalize(command.Title);
        banner.Description = Normalize(command.Description);
        banner.ButtonText = Normalize(command.ButtonText);
        banner.LinkUrl = Normalize(command.LinkUrl);
        banner.DisplayOrder = command.DisplayOrder;
        banner.IsActive = command.IsActive;
        banner.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(banner);
    }

    public async Task<bool> DeleteAsync(
        Guid bannerId,
        CancellationToken cancellationToken = default
    )
    {
        var banner = await dbContext.HomeBanners.SingleOrDefaultAsync(
            item => item.Id == bannerId,
            cancellationToken
        );
        if (banner is null)
        {
            return false;
        }

        dbContext.HomeBanners.Remove(banner);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<HomeBanner> Query() => dbContext.HomeBanners.AsNoTracking();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static HomeBannerDetails Map(HomeBanner banner) => new(
        banner.Id,
        banner.ImageUrl,
        banner.AltText,
        banner.Title,
        banner.Description,
        banner.ButtonText,
        banner.LinkUrl,
        banner.DisplayOrder,
        banner.IsActive,
        banner.UpdatedAtUtc
    );
}
