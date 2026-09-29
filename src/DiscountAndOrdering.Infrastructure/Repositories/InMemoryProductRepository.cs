using System.Collections.Concurrent;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Interfaces;

namespace DiscountAndOrdering.Infrastructure.Repositories;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public Task<Product?> GetByIdAsync(Guid id)
    {
        _products.TryGetValue(id, out var product);
        return Task.FromResult(product);
    }

    public Task<IReadOnlyList<Product>> GetAllAsync()
    {
        IReadOnlyList<Product> all = _products.Values.ToList();
        return Task.FromResult(all);
    }

    public Task AddAsync(Product product)
    {
        _products[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product)
    {
        _products[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _products.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
