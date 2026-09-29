using DiscountAndOrdering.Application.Dtos;
using DiscountAndOrdering.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace DiscountAndOrdering.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var result = await _orderService.CheckoutAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.FailureMessage, failedItems = result.FailedItems });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Order!.OrderId }, result.Order);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }
}
