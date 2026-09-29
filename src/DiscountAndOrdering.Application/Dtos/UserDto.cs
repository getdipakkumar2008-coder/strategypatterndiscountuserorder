using DiscountAndOrdering.Domain.Enums;

namespace DiscountAndOrdering.Application.Dtos;

public sealed record UserDto(Guid Id, string Name, string Email, UserTier Tier);

public sealed record CreateUserRequest(string Name, string Email, UserTier? Tier);

public sealed record UpdateUserRequest(string Name, string Email, UserTier? Tier);
