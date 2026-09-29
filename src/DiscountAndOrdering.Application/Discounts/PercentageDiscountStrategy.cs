using DiscountAndOrdering.Domain.Interfaces;

namespace DiscountAndOrdering.Application.Discounts;

/// <summary>
/// Discount mechanism shared by every percentage-off tier. The percentage itself is a
/// configuration value (see <see cref="DiscountSettings"/>), not a compiled literal — a
/// new percentage-off tier needs a config entry, not a new class.
/// </summary>
public sealed class PercentageDiscountStrategy : IDiscountStrategy
{
    private readonly decimal _percentage;

    public PercentageDiscountStrategy(decimal percentage)
    {
        _percentage = percentage;
    }

    public decimal ApplyDiscount(decimal subtotal) => subtotal * (_percentage / 100m);
}
