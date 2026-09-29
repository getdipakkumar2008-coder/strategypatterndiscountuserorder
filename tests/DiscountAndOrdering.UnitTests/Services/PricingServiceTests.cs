using DiscountAndOrdering.Application.Discounts;
using DiscountAndOrdering.Application.Services;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Enums;
using DiscountAndOrdering.Domain.Interfaces;
using Xunit;

namespace DiscountAndOrdering.UnitTests.Services;

internal sealed class FixedDiscountStrategyResolver(Dictionary<UserTier, decimal> percentages) : IDiscountStrategyResolver
{
    public IDiscountStrategy Resolve(UserTier tier) => new PercentageDiscountStrategy(percentages[tier]);
}

public class PricingServiceTests
{
    private static readonly Dictionary<UserTier, decimal> Percentages = new()
    {
        [UserTier.Normal] = 0,
        [UserTier.Premium] = 20,
        [UserTier.SuperPremium] = 30,
        [UserTier.Platinum] = 60
    };

    [Theory]
    [InlineData(UserTier.Normal, 0, 0)]
    [InlineData(UserTier.Premium, 20, 4)]
    [InlineData(UserTier.SuperPremium, 30, 6)]
    [InlineData(UserTier.Platinum, 60, 12)]
    public void CalculatePricing_AppliesCorrectDiscountPerTier(
        UserTier tier, decimal expectedPercentage, decimal expectedDiscountAmount)
    {
        var service = new PricingService(new FixedDiscountStrategyResolver(Percentages));
        var lineItems = new List<OrderLineItem> { new(Guid.NewGuid(), "Widget", 10.00m, 2) };

        var result = service.CalculatePricing(lineItems, tier);

        Assert.Equal(20.00m, result.Subtotal);
        Assert.Equal(expectedPercentage, result.DiscountPercentage);
        Assert.Equal(expectedDiscountAmount, result.DiscountAmount);
        Assert.Equal(20.00m - expectedDiscountAmount, result.FinalTotal);
    }
}
