namespace DiscountAndOrdering.Application.Dtos;

public sealed record CheckoutItemFailure(Guid? ProductId, string Reason);

public sealed class CheckoutResult
{
    public OrderResultDto? Order { get; }
    public string? FailureMessage { get; }
    public IReadOnlyList<CheckoutItemFailure> FailedItems { get; }

    public bool IsSuccess => Order is not null;

    private CheckoutResult(OrderResultDto? order, string? failureMessage, IReadOnlyList<CheckoutItemFailure> failedItems)
    {
        Order = order;
        FailureMessage = failureMessage;
        FailedItems = failedItems;
    }

    public static CheckoutResult Success(OrderResultDto order) => new(order, null, Array.Empty<CheckoutItemFailure>());

    public static CheckoutResult Failed(string message, IReadOnlyList<CheckoutItemFailure> failedItems) =>
        new(null, message, failedItems);
}
