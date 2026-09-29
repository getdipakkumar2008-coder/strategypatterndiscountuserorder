using DiscountAndOrdering.Domain.Entities;

namespace DiscountAndOrdering.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
