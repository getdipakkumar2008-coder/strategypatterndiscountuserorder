namespace DiscountAndOrdering.Application.Discounts;

public sealed class DiscountSettings
{
    // key = UserTier name (e.g. "Normal", "Premium", "SuperPremium"), value = percentage (0-100)
    public Dictionary<string, decimal> TierPercentages { get; set; } = new();
}
