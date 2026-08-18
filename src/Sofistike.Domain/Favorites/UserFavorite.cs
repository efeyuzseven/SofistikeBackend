using Sofistike.Domain.Catalog;
using Sofistike.Domain.Users;

namespace Sofistike.Domain.Favorites;

public sealed class UserFavorite
{
    public Guid UserId { get; set; }
    public UserAccount User { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
