namespace DiscountAndOrdering.Domain.Entities;

public sealed class OrderLineItem
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }
    public decimal LineTotal => UnitPrice * Quantity;

    public OrderLineItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name snapshot is required.", nameof(productName));
        if (unitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be greater than zero.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
