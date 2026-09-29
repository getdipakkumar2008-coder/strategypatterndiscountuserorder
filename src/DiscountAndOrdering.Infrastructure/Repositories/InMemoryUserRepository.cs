using System.Collections.Concurrent;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Interfaces;

namespace DiscountAndOrdering.Infrastructure.Repositories;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public Task<User?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}
