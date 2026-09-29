using System.Collections.Concurrent;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Interfaces;

namespace DiscountAndOrdering.Infrastructure.Repositories;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Task<Order?> GetByIdAsync(Guid id)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId)
    {
        IReadOnlyList<Order> matches = _orders.Values.Where(o => o.UserId == userId).ToList();
        return Task.FromResult(matches);
    }

    public Task AddAsync(Order order)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
