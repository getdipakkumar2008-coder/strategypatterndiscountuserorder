using DiscountAndOrdering.Application.Dtos;
using DiscountAndOrdering.Domain.Entities;
using DiscountAndOrdering.Domain.Interfaces;

namespace DiscountAndOrdering.Application.Services;

public sealed class OrderService
{
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly PricingService _pricingService;

    public OrderService(
        IUserRepository userRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        PricingService pricingService)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _pricingService = pricingService;
    }

    public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request)
    {
        if (request.Items.Count == 0)
            return CheckoutResult.Failed("The cart must contain at least one item.", Array.Empty<CheckoutItemFailure>());

        // FR-015 requires a real user to resolve a tier from; an unknown user is treated the
        // same way as an unresolvable checkout, even though FR-007 enumerates only product-
        // related failure reasons — checkout is meaningless without a real user.
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user is null)
            return CheckoutResult.Failed("Unknown user.", Array.Empty<CheckoutItemFailure>());

        // Sum duplicate productId entries into one requested quantity per product (spec.md Assumptions).
        var requestedQuantities = new Dictionary<Guid, int>();
        foreach (var item in request.Items)
            requestedQuantities[item.ProductId] = requestedQuantities.GetValueOrDefault(item.ProductId) + item.Quantity;

        var failures = new List<CheckoutItemFailure>();
        var resolvedProducts = new Dictionary<Guid, Product>();

        foreach (var (productId, quantity) in requestedQuantities)
        {
            if (quantity <= 0)
            {
                failures.Add(new CheckoutItemFailure(productId, "InvalidQuantity"));
                continue;
            }

            var product = await _productRepository.GetByIdAsync(productId);
            if (product is null)
            {
                failures.Add(new CheckoutItemFailure(productId, "UnknownProduct"));
                continue;
            }

            if (quantity > product.StockQuantity)
            {
                failures.Add(new CheckoutItemFailure(productId, "InsufficientStock"));
                continue;
            }

            resolvedProducts[productId] = product;
        }

        if (failures.Count > 0)
            return CheckoutResult.Failed("One or more items could not be fulfilled.", failures);

        var lineItems = requestedQuantities
            .Select(kvp => new OrderLineItem(kvp.Key, resolvedProducts[kvp.Key].Name, resolvedProducts[kvp.Key].Price, kvp.Value))
            .ToList();

        var pricing = _pricingService.CalculatePricing(lineItems, user.Tier);

        var order = new Order(
            Guid.NewGuid(),
            user.Id,
            DateTimeOffset.UtcNow,
            lineItems,
            pricing.Subtotal,
            pricing.DiscountPercentage,
            pricing.DiscountAmount,
            pricing.FinalTotal);

        await _orderRepository.AddAsync(order);

        foreach (var (productId, quantity) in requestedQuantities)
        {
            var product = resolvedProducts[productId];
            product.ReduceStock(quantity);
            await _productRepository.UpdateAsync(product);
        }

        return CheckoutResult.Success(ToDto(order, user.Tier));
    }

    public async Task<OrderResultDto?> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
            return null;

        var user = await _userRepository.GetByIdAsync(order.UserId);
        return ToDto(order, user?.Tier);
    }

    public async Task<IReadOnlyList<OrderResultDto>> GetByUserIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var orders = await _orderRepository.GetByUserIdAsync(userId);
        return orders.Select(o => ToDto(o, user?.Tier)).ToList();
    }

    private static OrderResultDto ToDto(Order order, Domain.Enums.UserTier? userTier) => new(
        order.Id,
        order.UserId,
        userTier ?? default,
        order.OrderDate,
        order.LineItems.Select(li => new OrderLineItemDto(li.ProductId, li.ProductName, li.UnitPrice, li.Quantity, li.LineTotal)).ToList(),
        order.Subtotal,
        order.DiscountPercentage,
        order.DiscountAmount,
        order.FinalTotal);
}
