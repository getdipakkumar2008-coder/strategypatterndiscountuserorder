namespace DiscountAndOrdering.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public DateTimeOffset OrderDate { get; }
    public IReadOnlyList<OrderLineItem> LineItems { get; }
    public decimal Subtotal { get; }
    public decimal DiscountPercentage { get; }
    public decimal DiscountAmount { get; }
    public decimal FinalTotal { get; }

    public Order(
        Guid id,
        Guid userId,
        DateTimeOffset orderDate,
        IReadOnlyList<OrderLineItem> lineItems,
        decimal subtotal,
        decimal discountPercentage,
        decimal discountAmount,
        decimal finalTotal)
    {
        if (lineItems is null || lineItems.Count == 0)
            throw new ArgumentException("An order must contain at least one line item.", nameof(lineItems));

        Id = id;
        UserId = userId;
        OrderDate = orderDate;
        LineItems = lineItems;
        Subtotal = subtotal;
        DiscountPercentage = discountPercentage;
        DiscountAmount = discountAmount;
        FinalTotal = finalTotal;
    }
}
