using DiscountAndOrdering.Domain.Enums;
using DiscountAndOrdering.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace DiscountAndOrdering.Application.Discounts;

public sealed class ConfigurableDiscountStrategyResolver : IDiscountStrategyResolver
{
    private readonly DiscountSettings _settings;

    public ConfigurableDiscountStrategyResolver(IOptionsSnapshot<DiscountSettings> options)
    {
        _settings = options.Value;
    }

    public IDiscountStrategy Resolve(UserTier tier)
    {
        if (!_settings.TierPercentages.TryGetValue(tier.ToString(), out var percentage))
            throw new InvalidOperationException($"No discount configuration for tier '{tier}'.");

        return new PercentageDiscountStrategy(percentage);
    }
}
