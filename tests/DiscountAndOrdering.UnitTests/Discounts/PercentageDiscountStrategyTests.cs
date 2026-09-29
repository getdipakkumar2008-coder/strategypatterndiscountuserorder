using DiscountAndOrdering.Application.Discounts;
using Xunit;

namespace DiscountAndOrdering.UnitTests.Discounts;

public class PercentageDiscountStrategyTests
{
    [Theory]
    [InlineData(0, 100.00, 0.00)]
    [InlineData(20, 100.00, 20.00)]
    [InlineData(30, 100.00, 30.00)]
    public void ApplyDiscount_ReturnsExpectedAmount(decimal percentage, decimal subtotal, decimal expectedDiscount)
    {
        var strategy = new PercentageDiscountStrategy(percentage);

        var discount = strategy.ApplyDiscount(subtotal);

        Assert.Equal(expectedDiscount, discount);
    }
}
