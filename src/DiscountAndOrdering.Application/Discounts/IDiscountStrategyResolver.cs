using DiscountAndOrdering.Domain.Enums;
using DiscountAndOrdering.Domain.Interfaces;

namespace DiscountAndOrdering.Application.Discounts;

public interface IDiscountStrategyResolver
{
    IDiscountStrategy Resolve(UserTier tier);
}
