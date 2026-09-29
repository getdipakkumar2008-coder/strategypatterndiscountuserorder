using DiscountAndOrdering.Domain.Entities;

namespace DiscountAndOrdering.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId);
    Task AddAsync(Order order);
}
