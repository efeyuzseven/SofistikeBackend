namespace Sofistike.Domain.Content;

public sealed class HomeBanner
{
    public Guid Id { get; set; }
    public required string ImageUrl { get; set; }
    public required string AltText { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ButtonText { get; set; }
    public string? LinkUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
