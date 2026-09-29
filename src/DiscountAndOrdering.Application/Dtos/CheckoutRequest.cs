namespace DiscountAndOrdering.Application.Dtos;

public sealed record CheckoutItem(Guid ProductId, int Quantity);

public sealed record CheckoutRequest(Guid UserId, IReadOnlyList<CheckoutItem> Items);
