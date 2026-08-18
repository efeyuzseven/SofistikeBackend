using Sofistike.Domain.Catalog;

namespace Sofistike.Infrastructure.Sales;

internal static class SalesPricing
{
    public static (decimal Amount, string CurrencyCode)? GetEffectivePrice(
        ProductVariant variant,
        DateTimeOffset now
    )
    {
        var listPrice = variant.Prices
            .Where(price =>
                price.ValidFromUtc <= now
                && (price.ValidToUtc is null || price.ValidToUtc > now))
            .OrderByDescending(price => price.ValidFromUtc)
            .FirstOrDefault();
        if (listPrice is null)
        {
            return null;
        }

        var campaignPrice = variant.CampaignPrices
            .Where(price =>
                price.Campaign.IsActive
                && price.Campaign.StartsAtUtc <= now
                && price.Campaign.EndsAtUtc > now
                && price.DiscountedAmount < listPrice.Amount)
            .OrderBy(price => price.DiscountedAmount)
            .FirstOrDefault();

        return (
            campaignPrice?.DiscountedAmount ?? listPrice.Amount,
            listPrice.CurrencyCode
        );
    }

    public static int GetAvailableQuantity(ProductVariant variant)
    {
        return variant.Stocks.Sum(stock =>
            stock.OnHandQuantity - stock.ReservedQuantity);
    }
}
