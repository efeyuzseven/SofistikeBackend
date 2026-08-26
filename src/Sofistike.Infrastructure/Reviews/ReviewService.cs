using Microsoft.EntityFrameworkCore;
using Sofistike.Application.Reviews;
using Sofistike.Domain.Reviews;
using Sofistike.Infrastructure.Persistence;

namespace Sofistike.Infrastructure.Reviews;

public sealed class ReviewService(SofistikeDbContext dbContext) : IReviewService
{
    public async Task<IReadOnlyList<ReviewSummary>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await Query()
            .Where(item => item.Review.UserId == userId)
            .OrderByDescending(item => item.Review.CreatedAtUtc)
            .Select(item => Map(
                item.Review,
                item.OrderNumber,
                item.ProductName
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<CreateReviewResult> CreateAsync(
        Guid userId,
        CreateReviewCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(
                item => item.Id == command.OrderId && item.UserId == userId,
                cancellationToken
            );
        if (order is null)
        {
            return new CreateReviewResult(CreateReviewStatus.OrderNotFound, null);
        }

        string? productName = null;
        if (command.ProductId.HasValue)
        {
            productName = order.Items
                .FirstOrDefault(item => item.ProductId == command.ProductId.Value)
                ?.ProductName;
            if (productName is null)
            {
                return new CreateReviewResult(
                    CreateReviewStatus.ProductNotInOrder,
                    null
                );
            }
        }

        var type = command.ProductId.HasValue
            ? ReviewType.Product
            : ReviewType.Order;
        if (await dbContext.ProductReviews.AnyAsync(
            review =>
                review.UserId == userId
                && review.OrderId == command.OrderId
                && review.ProductId == command.ProductId
                && review.Type == type,
            cancellationToken
        ))
        {
            return new CreateReviewResult(CreateReviewStatus.Duplicate, null);
        }

        var now = DateTimeOffset.UtcNow;
        var review = new ProductReview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderId = command.OrderId,
            ProductId = command.ProductId,
            Type = type,
            Rating = command.Rating,
            Comment = command.Comment.Trim(),
            IsAnonymous = command.IsAnonymous,
            Status = ReviewStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        dbContext.ProductReviews.Add(review);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (await dbContext.ProductReviews.AnyAsync(
                item =>
                    item.UserId == userId
                    && item.OrderId == command.OrderId
                    && item.ProductId == command.ProductId
                    && item.Type == type,
                cancellationToken
            ))
            {
                return new CreateReviewResult(CreateReviewStatus.Duplicate, null);
            }

            throw;
        }

        return new CreateReviewResult(
            CreateReviewStatus.Created,
            Map(review, order.OrderNumber, productName)
        );
    }

    private IQueryable<ReviewProjection> Query()
    {
        return from review in dbContext.ProductReviews.AsNoTracking()
               join order in dbContext.Orders.AsNoTracking()
                   on review.OrderId equals order.Id
               join product in dbContext.Products.AsNoTracking()
                   on review.ProductId equals product.Id into products
               from product in products.DefaultIfEmpty()
               select new ReviewProjection
               {
                   Review = review,
                   OrderNumber = order.OrderNumber,
                   ProductName = product == null ? null : product.Name,
               };
    }

    private static ReviewSummary Map(
        ProductReview review,
        string orderNumber,
        string? productName
    )
    {
        return new ReviewSummary(
            review.Id,
            review.OrderId,
            orderNumber,
            review.ProductId,
            productName,
            review.Type.ToString(),
            review.Rating,
            review.Comment,
            review.IsAnonymous,
            review.Status.ToString(),
            review.CreatedAtUtc
        );
    }

    private sealed class ReviewProjection
    {
        public required ProductReview Review { get; init; }
        public required string OrderNumber { get; init; }
        public string? ProductName { get; init; }
    }
}
