namespace DiscountAndOrdering.Application.Dtos;

public sealed record ProductDto(Guid Id, string Name, string Description, decimal Price, int StockQuantity);

public sealed record CreateProductRequest(string Name, string? Description, decimal Price, int StockQuantity);
