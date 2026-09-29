using DiscountAndOrdering.Application.Dtos;
using DiscountAndOrdering.Application.Exceptions;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Interfaces;

namespace DiscountAndOrdering.Application.Services;

public sealed class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        if (request.Tier is null)
            throw new ValidationException("A membership tier is required.");

        var user = new User(Guid.NewGuid(), request.Name, request.Email, request.Tier.Value);
        await _userRepository.AddAsync(user);

        return ToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        if (request.Tier is null)
            throw new ValidationException("A membership tier is required.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            return null;

        user.UpdateDetails(request.Name, request.Email, request.Tier.Value);
        await _userRepository.UpdateAsync(user);

        return ToDto(user);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user is null ? null : ToDto(user);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Name, user.Email, user.Tier);
}
