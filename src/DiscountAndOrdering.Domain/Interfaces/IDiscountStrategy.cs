namespace DiscountAndOrdering.Domain.Interfaces;

public interface IDiscountStrategy
{
    decimal ApplyDiscount(decimal subtotal);
}
