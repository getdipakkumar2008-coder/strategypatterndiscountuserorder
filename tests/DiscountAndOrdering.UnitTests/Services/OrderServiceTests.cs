using DiscountAndOrdering.Application.Discounts;
using DiscountAndOrdering.Application.Dtos;
using DiscountAndOrdering.Application.Services;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Enums;
using DiscountAndOrdering.Domain.Interfaces;
using Xunit;

namespace DiscountAndOrdering.UnitTests.Services;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();
    public void Seed(User user) => _users[user.Id] = user;
    public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(_users.GetValueOrDefault(id));
    public Task AddAsync(User user) { _users[user.Id] = user; return Task.CompletedTask; }
    public Task UpdateAsync(User user) { _users[user.Id] = user; return Task.CompletedTask; }
}

internal sealed class FakeProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = new();
    public void Seed(Product product) => _products[product.Id] = product;
    public int UpdateCallCount { get; private set; }
    public Task<Product?> GetByIdAsync(Guid id) => Task.FromResult(_products.GetValueOrDefault(id));
    public Task<IReadOnlyList<Product>> GetAllAsync() => Task.FromResult<IReadOnlyList<Product>>(_products.Values.ToList());
    public Task AddAsync(Product product) { _products[product.Id] = product; return Task.CompletedTask; }
    public Task UpdateAsync(Product product) { UpdateCallCount++; _products[product.Id] = product; return Task.CompletedTask; }
    public Task DeleteAsync(Guid id) { _products.Remove(id); return Task.CompletedTask; }
}

internal sealed class FakeOrderRepository : IOrderRepository
{
    public List<Order> Orders { get; } = new();
    public Task<Order?> GetByIdAsync(Guid id) => Task.FromResult(Orders.FirstOrDefault(o => o.Id == id));
    public Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId) =>
        Task.FromResult<IReadOnlyList<Order>>(Orders.Where(o => o.UserId == userId).ToList());
    public Task AddAsync(Order order) { Orders.Add(order); return Task.CompletedTask; }
}

public class OrderServiceTests
{
    private static (OrderService Service, FakeUserRepository Users, FakeProductRepository Products, FakeOrderRepository Orders) CreateSut()
    {
        var users = new FakeUserRepository();
        var products = new FakeProductRepository();
        var orders = new FakeOrderRepository();
        var resolver = new FixedDiscountStrategyResolver(new Dictionary<UserTier, decimal>
        {
            [UserTier.Normal] = 0,
            [UserTier.Premium] = 20,
            [UserTier.SuperPremium] = 30
        });
        var pricing = new PricingService(resolver);
        var service = new OrderService(users, products, orders, pricing);
        return (service, users, products, orders);
    }

    [Fact]
    public async Task CheckoutAsync_RejectsEmptyCart_NoOrderCreated()
    {
        var (service, users, _, orders) = CreateSut();
        var user = new User(Guid.NewGuid(), "Ada", "ada@example.com", UserTier.Normal);
        users.Seed(user);

        var result = await service.CheckoutAsync(new CheckoutRequest(user.Id, Array.Empty<CheckoutItem>()));

        Assert.False(result.IsSuccess);
        Assert.Empty(orders.Orders);
    }

    [Fact]
    public async Task CheckoutAsync_RejectsUnknownProduct_IdentifiesFailingItem_NoOrderCreated()
    {
        var (service, users, _, orders) = CreateSut();
        var user = new User(Guid.NewGuid(), "Ada", "ada@example.com", UserTier.Normal);
        users.Seed(user);
        var unknownProductId = Guid.NewGuid();

        var result = await service.CheckoutAsync(new CheckoutRequest(user.Id, new[] { new CheckoutItem(unknownProductId, 1) }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.FailedItems, f => f.ProductId == unknownProductId && f.Reason == "UnknownProduct");
        Assert.Empty(orders.Orders);
    }

    [Fact]
    public async Task CheckoutAsync_RejectsInsufficientStock_LeavesStockUnchanged_NoOrderCreated()
    {
        var (service, users, products, orders) = CreateSut();
        var user = new User(Guid.NewGuid(), "Ada", "ada@example.com", UserTier.Normal);
        users.Seed(user);
        var product = new Product(Guid.NewGuid(), "Widget", "desc", 10.00m, 2);
        products.Seed(product);

        var result = await service.CheckoutAsync(new CheckoutRequest(user.Id, new[] { new CheckoutItem(product.Id, 5) }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.FailedItems, f => f.ProductId == product.Id && f.Reason == "InsufficientStock");
        Assert.Empty(orders.Orders);
        Assert.Equal(2, product.StockQuantity);
        Assert.Equal(0, products.UpdateCallCount);
    }

    [Fact]
    public async Task CheckoutAsync_RejectsNonPositiveQuantity_NoOrderCreated()
    {
        var (service, users, products, orders) = CreateSut();
        var user = new User(Guid.NewGuid(), "Ada", "ada@example.com", UserTier.Normal);
        users.Seed(user);
        var product = new Product(Guid.NewGuid(), "Widget", "desc", 10.00m, 5);
        products.Seed(product);

        var result = await service.CheckoutAsync(new CheckoutRequest(user.Id, new[] { new CheckoutItem(product.Id, 0) }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.FailedItems, f => f.ProductId == product.Id && f.Reason == "InvalidQuantity");
        Assert.Empty(orders.Orders);
    }

    [Fact]
    public async Task CheckoutAsync_SumsDuplicateProductEntries_SucceedsAndReducesStockOnce()
    {
        var (service, users, products, orders) = CreateSut();
        var user = new User(Guid.NewGuid(), "Ada", "ada@example.com", UserTier.Premium);
        users.Seed(user);
        var product = new Product(Guid.NewGuid(), "Widget", "desc", 10.00m, 10);
        products.Seed(product);

        var result = await service.CheckoutAsync(new CheckoutRequest(
            user.Id, new[] { new CheckoutItem(product.Id, 2), new CheckoutItem(product.Id, 3) }));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Order!.LineItems);
        Assert.Equal(5, result.Order.LineItems[0].Quantity);
        Assert.Equal(50.00m, result.Order.Subtotal);
        Assert.Equal(20, result.Order.DiscountPercentage);
        Assert.Equal(10.00m, result.Order.DiscountAmount);
        Assert.Equal(40.00m, result.Order.FinalTotal);
        Assert.Equal(5, product.StockQuantity);
        Assert.Single(orders.Orders);
    }
}
