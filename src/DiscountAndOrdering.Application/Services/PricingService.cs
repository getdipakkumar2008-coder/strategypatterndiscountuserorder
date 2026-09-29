using DiscountAndOrdering.Application.Discounts;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Enums;

namespace DiscountAndOrdering.Application.Services;

public sealed record PricingResult(decimal Subtotal, decimal DiscountPercentage, decimal DiscountAmount, decimal FinalTotal);

public sealed class PricingService
{
    private readonly IDiscountStrategyResolver _discountStrategyResolver;

    public PricingService(IDiscountStrategyResolver discountStrategyResolver)
    {
        _discountStrategyResolver = discountStrategyResolver;
    }

    public PricingResult CalculatePricing(IReadOnlyList<OrderLineItem> lineItems, UserTier userTier)
    {
        var subtotal = lineItems.Sum(item => item.LineTotal);

        var strategy = _discountStrategyResolver.Resolve(userTier);
        var discountAmount = strategy.ApplyDiscount(subtotal);

        // IDiscountStrategy is mechanism-agnostic (no "percentage" concept for e.g. a future
        // flat-amount strategy), so the percentage recorded on the order is derived here
        // rather than exposed on the interface. Subtotal is always > 0 (an order requires at
        // least one line item, and every line item has a positive price and quantity).
        var discountPercentage = discountAmount / subtotal * 100m;

        return new PricingResult(subtotal, discountPercentage, discountAmount, subtotal - discountAmount);
    }
}
