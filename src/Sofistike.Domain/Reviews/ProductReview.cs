namespace Sofistike.Domain.Reviews;

public enum ReviewType
{
    Product,
    Order,
}

public enum ReviewStatus
{
    Pending,
    Published,
    Rejected,
}

public sealed class ProductReview
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public ReviewType Type { get; set; }
    public int Rating { get; set; }
    public required string Comment { get; set; }
    public bool IsAnonymous { get; set; }
    public ReviewStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
