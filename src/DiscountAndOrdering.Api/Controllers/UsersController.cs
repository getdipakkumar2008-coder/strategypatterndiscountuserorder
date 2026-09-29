using DiscountAndOrdering.Application.Dtos;
using DiscountAndOrdering.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DiscountAndOrdering.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly OrderService _orderService;

    public UsersController(UserService userService, OrderService orderService)
    {
        _userService = userService;
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateAsync(id, request);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("{id:guid}/orders")]
    public async Task<IActionResult> GetOrders(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null)
            return NotFound();

        var orders = await _orderService.GetByUserIdAsync(id);
        return Ok(orders);
    }
}
