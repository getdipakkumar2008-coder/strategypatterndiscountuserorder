using DiscountAndOrdering.Application.Discounts;
using DiscountAndOrdering.Domain.Enums;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiscountAndOrdering.UnitTests.Discounts;

/// <summary>Minimal IOptionsSnapshot fake — no DI container or config file needed for the test.</summary>
internal sealed class FakeOptionsSnapshot<T>(T value) : IOptionsSnapshot<T> where T : class
{
    public T Value { get; } = value;
    public T Get(string? name) => Value;
}

public class ConfigurableDiscountStrategyResolverTests
{
    private static ConfigurableDiscountStrategyResolver CreateResolver(Dictionary<string, decimal> tierPercentages)
    {
        var settings = new DiscountSettings { TierPercentages = tierPercentages };
        return new ConfigurableDiscountStrategyResolver(new FakeOptionsSnapshot<DiscountSettings>(settings));
    }

    [Theory]
    [InlineData(UserTier.Normal, 0)]
    [InlineData(UserTier.Premium, 20)]
    [InlineData(UserTier.SuperPremium, 30)]
    [InlineData(UserTier.Platinum, 60)]
    public void Resolve_UsesConfiguredPercentageForTier(UserTier tier, decimal expectedPercentage)
    {
        var resolver = CreateResolver(new Dictionary<string, decimal>
        {
            ["Normal"] = 0,
            ["Premium"] = 20,
            ["SuperPremium"] = 30,
            ["Platinum"] = 60
        });

        var strategy = resolver.Resolve(tier);

        Assert.Equal(expectedPercentage, strategy.ApplyDiscount(100m));
    }

    [Fact]
    public void Resolve_ThrowsWhenTierHasNoConfiguredEntry()
    {
        var resolver = CreateResolver(new Dictionary<string, decimal> { ["Normal"] = 0 });

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(UserTier.Premium));
    }
}
