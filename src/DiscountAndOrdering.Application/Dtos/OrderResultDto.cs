using DiscountAndOrdering.Domain.Enums;

namespace DiscountAndOrdering.Application.Dtos;

public sealed record OrderLineItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderResultDto(
    Guid OrderId,
    Guid UserId,
    UserTier UserTier,
    DateTimeOffset OrderDate,
    IReadOnlyList<OrderLineItemDto> LineItems,
    decimal Subtotal,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal FinalTotal);
