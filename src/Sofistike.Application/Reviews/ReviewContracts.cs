namespace Sofistike.Application.Reviews;

public sealed record CreateReviewCommand(
    Guid OrderId,
    Guid? ProductId,
    int Rating,
    string Comment,
    bool IsAnonymous
);

public sealed record ReviewSummary(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    Guid? ProductId,
    string? ProductName,
    string Type,
    int Rating,
    string Comment,
    bool IsAnonymous,
    string Status,
    DateTimeOffset CreatedAtUtc
);

public enum CreateReviewStatus
{
    Created,
    OrderNotFound,
    ProductNotInOrder,
    Duplicate,
}

public sealed record CreateReviewResult(
    CreateReviewStatus Status,
    ReviewSummary? Review
);

public interface IReviewService
{
    Task<IReadOnlyList<ReviewSummary>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<CreateReviewResult> CreateAsync(
        Guid userId,
        CreateReviewCommand command,
        CancellationToken cancellationToken = default
    );
}
