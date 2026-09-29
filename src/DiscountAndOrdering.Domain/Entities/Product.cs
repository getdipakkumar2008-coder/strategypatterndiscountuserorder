namespace DiscountAndOrdering.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    public Product(Guid id, string name, string description, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");

        Id = id;
        Name = name;
        Description = description ?? string.Empty;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public void UpdateDetails(string name, string description, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");

        Name = name;
        Description = description ?? string.Empty;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity to reduce must be positive.");
        if (quantity > StockQuantity)
            throw new InvalidOperationException("Cannot reduce stock below zero.");

        StockQuantity -= quantity;
    }
}
